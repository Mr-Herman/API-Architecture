using System.Data.Common;
using EAP.DataAccess.Options;
using EAP.DataAccess.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;

namespace EAP.DataAccess.Database;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly IConnectionStringProtector _connectionStringProtector;

    public DbConnectionFactory(IOptions<DatabaseOptions> options, IConnectionStringProtector connectionStringProtector)
    {
        _options = options.Value;
        _connectionStringProtector = connectionStringProtector;
    }

    public DbConnection CreateConnection(string connectionName)
    {
        var registration = ResolveRegistration(connectionName);

        if (!registration.Enabled)
        {
            throw new InvalidOperationException($"Database connection '{connectionName}' is disabled.");
        }

        if (string.IsNullOrWhiteSpace(registration.ConnectionString))
        {
            throw new InvalidOperationException($"Database connection '{connectionName}' has no connection string.");
        }

        var connectionString = _connectionStringProtector.Decrypt(registration.ConnectionString);
        var provider = ParseProvider(registration.Provider);
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                return new SqlConnection(connectionString);
            case DatabaseProvider.Oracle:
                return new OracleConnection(connectionString);
            case DatabaseProvider.MySql:
                return new MySqlConnection(connectionString);

            default:
                throw new NotSupportedException($"Database provider '{registration.Provider}' is not supported.");
        }
    }

    public async Task<DbConnection> OpenConnectionAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection(connectionName);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public int GetCommandTimeoutSeconds(string connectionName)
    {
        return ResolveRegistration(connectionName).CommandTimeoutSeconds;
    }

    public static bool IsSupportedProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return false;
        if (string.Equals(provider, nameof(DatabaseProvider.SqlServer), StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(provider, nameof(DatabaseProvider.Oracle), StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(provider, nameof(DatabaseProvider.MySql), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private DatabaseConnectionOptions ResolveRegistration(string connectionName)
    {
        var registration = _options.Connections.FirstOrDefault(pair =>
            string.Equals(pair.Key, connectionName, StringComparison.OrdinalIgnoreCase));
        return registration.Value
            ?? throw new KeyNotFoundException($"Database connection '{connectionName}' is not registered.");
    }

    private static DatabaseProvider ParseProvider(string provider)
    {
        if (string.Equals(provider, nameof(DatabaseProvider.SqlServer), StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.SqlServer;
        if (string.Equals(provider, nameof(DatabaseProvider.Oracle), StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Oracle;
        if (string.Equals(provider, nameof(DatabaseProvider.MySql), StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.MySql;

        throw new NotSupportedException($"Database provider '{provider}' is not supported.");
    }
}
