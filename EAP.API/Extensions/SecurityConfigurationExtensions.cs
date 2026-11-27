namespace EAP.Api.Extensions;

public static class SecurityConfigurationExtensions
{
    public static WebApplicationBuilder AddSecuritySecrets(
        this WebApplicationBuilder builder)
    {
        var securitySecretsFile =
            builder.Configuration["SecuritySecrets:FilePath"];

        if (string.IsNullOrWhiteSpace(securitySecretsFile))
        {
            return builder;
        }

        if (!Path.IsPathRooted(securitySecretsFile))
        {
            securitySecretsFile = Path.GetFullPath(
                securitySecretsFile,
                builder.Environment.ContentRootPath);
        }

        builder.Configuration.AddJsonFile(
            securitySecretsFile,
            optional: false,
            reloadOnChange: false);

        return builder;
    }
}
