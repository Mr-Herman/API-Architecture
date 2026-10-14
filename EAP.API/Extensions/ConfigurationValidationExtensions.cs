using EAP.DataAccess.Database;
using EAP.DataAccess.Options;
using EAP.DataAccess.Security;
using EAP.Infrastructure.Options;

namespace EAP.Api.Extensions;

public static class ConfigurationValidationExtensions
{
    public static IServiceCollection AddApiConfigurationValidation(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .PostConfigure(options => ResolveKeyFilePath(options, contentRootPath))
            .Validate(options => !string.IsNullOrWhiteSpace(options.KeyFilePath),
                "AES key file path is required.")
            .Validate(options => File.Exists(options.KeyFilePath),
                "AES key file does not exist.")
            .Validate(HasValidAesKey,
                "AES key file must contain a Base64 encoded 32-byte key.")
            .ValidateOnStart();

        services.AddOptions<HttpApiOptions>()
            .Bind(configuration.GetSection(HttpApiOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is > 0 and <= 300,
                "HTTP API timeout must be between 1 and 300 seconds.")
            .Validate(options => options.RetryCount is >= 0 and <= 10,
                "HTTP API retry count must be between 0 and 10.")
            .Validate(HasHttpEndpoints,
                "At least one HTTP API endpoint must be configured.")
            .Validate(HttpEndpointsHaveValidUrls,
                "Every HTTP API endpoint must be an absolute HTTP or HTTPS URI.")
            .Validate(HttpEndpointsHaveSupportedMethods,
                "HTTP API endpoint default methods must be GET or POST.")
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(HasDatabaseConnections,
                "At least one database connection must be registered.")
            .Validate(DatabaseProvidersAreSupported,
                "Database providers must be SqlServer, Oracle or MySql.")
            .Validate(DatabaseHealthChecksAreValid,
                "Database health checks can only be enabled for enabled connections.")
            .Validate(DatabaseCommandTimeoutsAreValid,
                "Database command timeout must be between 1 and 1800 seconds.")
            .Validate(EnabledDatabasesHaveConnectionStrings,
                "Every enabled database requires a configured connection string.")
            .Validate(ConfiguredConnectionStringsAreEncrypted,
                "Every configured database connection string must use the enc:v1: format.")
            .ValidateOnStart();

        return services;
    }

    private static void ResolveKeyFilePath(EncryptionOptions options, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(options.KeyFilePath) || Path.IsPathRooted(options.KeyFilePath))
        {
            return;
        }

        options.KeyFilePath = Path.GetFullPath(
            options.KeyFilePath,
            contentRootPath);
    }

    private static bool HasValidAesKey(EncryptionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.KeyFilePath) || !File.Exists(options.KeyFilePath))
        {
            return false;
        }

        try
        {
            var encodedKey = File.ReadAllText(options.KeyFilePath).Trim();
            return Convert.FromBase64String(encodedKey).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool HasHttpEndpoints(HttpApiOptions options)
    {
        return options.Endpoints is { Count: > 0 };
    }

    private static bool HttpEndpointsHaveValidUrls(HttpApiOptions options)
    {
        return options.Endpoints is not null
            && options.Endpoints.Values.All(endpoint =>
                endpoint is not null && IsAbsoluteHttpUrl(endpoint.Url));
    }

    private static bool IsAbsoluteHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool HttpEndpointsHaveSupportedMethods(HttpApiOptions options)
    {
        return options.Endpoints is not null
            && options.Endpoints.Values.All(endpoint =>
                endpoint is not null && IsSupportedHttpMethod(endpoint.DefaultMethod));
    }

    private static bool IsSupportedHttpMethod(string method)
    {
        return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDatabaseConnections(DatabaseOptions options)
    {
        return options.Connections is { Count: > 0 };
    }

    private static bool DatabaseProvidersAreSupported(DatabaseOptions options)
    {
        return options.Connections is not null
            && options.Connections.Values.All(connection =>
                connection is not null
                && DbConnectionFactory.IsSupportedProvider(connection.Provider));
    }

    private static bool DatabaseHealthChecksAreValid(DatabaseOptions options)
    {
        return options.Connections is not null
            && options.Connections.Values.All(connection =>
                connection is not null
                && (!connection.HealthCheckEnabled || connection.Enabled));
    }

    private static bool EnabledDatabasesHaveConnectionStrings(DatabaseOptions options)
    {
        return options.Connections is not null
            && options.Connections.Values.All(connection =>
                connection is not null
                && (!connection.Enabled
                    || !string.IsNullOrWhiteSpace(connection.ConnectionString)));
    }

    private static bool DatabaseCommandTimeoutsAreValid(DatabaseOptions options)
    {
        return options.Connections is not null
            && options.Connections.Values.All(connection =>
                connection is not null
                && connection.CommandTimeoutSeconds is >= 1 and <= 1800);
    }

    private static bool ConfiguredConnectionStringsAreEncrypted(DatabaseOptions options)
    {
        return options.Connections is not null
            && options.Connections.Values.All(connection =>
                connection is not null
                && (string.IsNullOrWhiteSpace(connection.ConnectionString)
                    || connection.ConnectionString.StartsWith(
                        AesConnectionStringProtector.EncryptedPrefix,
                        StringComparison.Ordinal)));
    }
}
