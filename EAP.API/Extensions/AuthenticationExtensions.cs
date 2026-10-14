using System.Text;
using EAP.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EAP.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration, bool authenticationRequired)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(HasValidSigningKey,
                "JWT signing key must contain at least 32 characters.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
                "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "JWT audience is required.")
            .Validate(options => options.ClockSkewSeconds is >= 0 and <= 300,
                "JWT clock skew must be between 0 and 300 seconds.")
            .Validate(options => options.AccessTokenMinutes is > 0 and <= 1440,
                "JWT lifetime must be between 1 and 1440 minutes.")
            .Validate(HasConfiguredClients,
                "At least one API client must be configured.")
            .Validate(ClientsHaveRequiredValues,
                "API client ID, secret and name are required.")
            .Validate(ClientsHaveSecureSecrets,
                "API client secrets must contain at least 16 characters.")
            .Validate(ClientIdsAreUnique,
                "API client IDs must be unique.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(ConfigureJwtBearer);

        services.AddAuthorization(options =>
        {
            var policy = CreateAuthorizationPolicy(authenticationRequired);
            options.DefaultPolicy = policy;
            options.FallbackPolicy = policy;
        });

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IOptions<JwtOptions> configuredOptions)
    {
        var jwt = configuredOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
            NameClaimType = "ClientName"
        };
    }

    private static AuthorizationPolicy CreateAuthorizationPolicy(bool authenticationRequired)
    {
        if (authenticationRequired)
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        }

        return new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    }

    private static bool HasValidSigningKey(JwtOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.SigningKey)
            && options.SigningKey.Length >= 32;
    }

    private static bool HasConfiguredClients(JwtOptions options)
    {
        return options.Clients is { Count: > 0 };
    }

    private static bool ClientsHaveRequiredValues(JwtOptions options)
    {
        return options.Clients is not null
            && options.Clients.All(client =>
                client is not null
                && !string.IsNullOrWhiteSpace(client.ClientId)
                && !string.IsNullOrWhiteSpace(client.ClientSecret)
                && !string.IsNullOrWhiteSpace(client.ClientName));
    }

    private static bool ClientsHaveSecureSecrets(JwtOptions options)
    {
        return options.Clients is not null
            && options.Clients.All(client =>
                client is not null
                && !string.IsNullOrWhiteSpace(client.ClientSecret)
                && client.ClientSecret.Length >= 16);
    }

    private static bool ClientIdsAreUnique(JwtOptions options)
    {
        if (options.Clients is null || options.Clients.Any(client => client is null))
        {
            return false;
        }

        var uniqueClientCount = options.Clients
            .Select(client => client.ClientId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return uniqueClientCount == options.Clients.Count;
    }
}
