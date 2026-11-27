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

    public static WebApplication UseApiStaticFiles(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (!context.Context.Request.Path.StartsWithSegments("/log-viewer"))
                {
                    return;
                }

                context.Context.Response.Headers.CacheControl = "no-cache, must-revalidate";
                context.Context.Response.Headers.Pragma = "no-cache";
            }
        });

        return app;
    }
}
