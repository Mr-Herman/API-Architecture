using EAP.DataAccess.Health;
using EAP.DataAccess.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EAP.Api.Extensions;

public static class HealthCheckExtensions
{
    private const string ReadinessTag = "ready";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();
        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        foreach (var database in databaseOptions.Connections)
        {
            var connection = database.Value;
            if (!connection.Enabled || !connection.HealthCheckEnabled)
            {
                continue;
            }

            healthChecks.AddTypeActivatedCheck<DatabaseHealthCheck>(
                $"database-{database.Key}",
                failureStatus: HealthStatus.Unhealthy,
                tags: new List<string> { ReadinessTag },
                args: new object[] { database.Key });
        }

        return services;
    }

    public static WebApplication MapApiHealthEndpoints(this WebApplication app)
    {
        if (app.IsApiDocumentationEnabled())
        {
            app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
                .AllowAnonymous()
                .ExcludeFromDescription();
        }
        else
        {
            app.MapGet("/", () => Results.Ok("EAP Web API is running."))
                .AllowAnonymous()
                .ExcludeFromDescription();
        }

        app.MapGet("/health/live", () => Results.Ok("Healthy"))
            .AllowAnonymous();

        var readinessOptions = new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains(ReadinessTag)
        };

        app.MapHealthChecks("/health/ready", readinessOptions)
            .AllowAnonymous();

        return app;
    }
}
