using EAP.Api.LogViewer;
using EAP.Api.Logging;

namespace EAP.Api.Extensions;

public static class LogViewerExtensions
{
    public static IServiceCollection AddApiLogViewer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LogViewerOptions>().Bind(configuration.GetSection(LogViewerOptions.SectionName));

        services.AddSingleton<LogEntryParser>();
        services.AddSingleton<HttpPayloadSanitizer>();
        services.AddSingleton<ILogFileReader, LogFileReader>();

        return services;
    }
}
