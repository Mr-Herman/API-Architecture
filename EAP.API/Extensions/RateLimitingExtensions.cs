using System.Threading.RateLimiting;
using EAP.Api.Options;
using EAP.Application.Localization;
using EAP.Models.Constants;
using EAP.Models.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace EAP.Api.Extensions;

public static class RateLimitingExtensions
{
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
        });

        return services;
    }

    private static async ValueTask WriteRateLimitResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IMessageLocalizer>();

        var result = ApiResult<object>.FailResult(
            localizer["TooManyRequests"],
            BusinessCode.TooManyRequests);

        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
    }

    private static RateLimitPartition<string> CreateRateLimitPartition(HttpContext context, RateLimitOptions options)
    {
        var clientKey = context.User.Identity?.Name
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
}
