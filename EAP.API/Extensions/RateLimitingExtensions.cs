using System.Security.Claims;
using System.Threading.RateLimiting;
using EAP.Api.Options;
using EAP.Api.RateLimiting;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace EAP.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string TokenPolicyName = "TokenEndpoint";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .Validate(options => options.PermitLimit is > 0 and <= 100000,
                "Rate limit permit count must be between 1 and 100000.")
            .Validate(options => options.WindowSeconds is > 0 and <= 3600,
                "Rate limit window must be between 1 and 3600 seconds.")
            .Validate(options => options.QueueLimit is >= 0 and <= 10000,
                "Rate limit queue count must be between 0 and 10000.")
            .Validate(HasValidEapClientOptions,
                "EAP client request protection configuration is invalid.")
            .Validate(HasValidTokenOptions,
                "Token request protection configuration is invalid.")
            .ValidateOnStart();

        var rateLimit = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRateLimitResponseAsync;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => CreateRateLimitPartition(context, rateLimit));
            options.AddPolicy(TokenPolicyName, context =>
                CreateTokenRateLimitPartition(context, rateLimit.Token));
        });

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEapClientRequestProtection, EapClientRequestProtectionService>();

        return services;
    }

    private static async ValueTask WriteRateLimitResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IMessageLocalizer>();

        var result = ApiResult<object>.FailResult(localizer["TooManyRequests"], 4007);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
    }

    private static RateLimitPartition<string> CreateTokenRateLimitPartition(HttpContext context, TokenRateLimitOptions options)
    {
        var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(
            clientKey,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = options.SystemTokenLimit,
                TokensPerPeriod = options.SystemTokensPerPeriod,
                ReplenishmentPeriod = TimeSpan.FromSeconds(options.SystemReplenishmentSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static RateLimitPartition<string> CreateRateLimitPartition(HttpContext context, RateLimitOptions options)
    {
        var clientKey = context.User.FindFirstValue(JwtClaimNames.EapClientId)
            ?? context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => CreateFixedWindowOptions(options));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(RateLimitOptions options)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = options.QueueLimit,
            AutoReplenishment = true
        };
    }

    private static bool HasValidEapClientOptions(RateLimitOptions options)
    {
        var eapClient = options.EapClient;
        return eapClient.DefaultIntervalSeconds is >= 1 and <= 60
            && eapClient.ObservationWindowSeconds is >= 1 and <= 3600
            && eapClient.RequestThreshold is >= 1 and <= 100000
            && eapClient.CooldownSeconds is >= 1 and <= 3600
            && eapClient.ViolationPeriodSeconds >= eapClient.ObservationWindowSeconds
            && eapClient.ViolationWindowsBeforeBan is >= 1 and <= 100
            && eapClient.BanSeconds is >= 1 and <= 86400
            && eapClient.StateExpirationMinutes is >= 1 and <= 1440
            && eapClient.RouteIntervalSeconds.Values.All(seconds => seconds is >= 1 and <= 60);
    }

    private static bool HasValidTokenOptions(RateLimitOptions options)
    {
        var token = options.Token;
        return token.PermitLimit is >= 1 and <= 100
            && token.WindowSeconds is >= 1 and <= 3600
            && token.BanSeconds is >= 1 and <= 86400
            && token.SystemTokenLimit is >= 1 and <= 10000
            && token.SystemTokensPerPeriod >= 1
            && token.SystemTokensPerPeriod <= token.SystemTokenLimit
            && token.SystemReplenishmentSeconds is >= 1 and <= 60;
    }
}
