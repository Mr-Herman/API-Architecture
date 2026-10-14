using EAP.DataAccess.Database;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oracle.ManagedDataAccess.Client;

namespace EAP.DataAccess.Health;

public sealed class DatabaseHealthCheck(IDbConnectionFactory connectionFactory, string connectionName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(connectionName, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = connection is OracleConnection
                ? "SELECT 1 FROM DUAL"
                : "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Database '{connectionName}' is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy($"Database '{connectionName}' is unavailable.", exception);
        }
    }
}
