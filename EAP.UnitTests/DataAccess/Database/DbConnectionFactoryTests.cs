using EAP.DataAccess.Database;
using EAP.DataAccess.Options;
using EAP.DataAccess.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;

namespace EAP.UnitTests.Tests;

public sealed class DbConnectionFactoryTests
{
    [Theory]
    [InlineData("Sql", "SqlServer", typeof(SqlConnection))]
    [InlineData("Oracle", "Oracle", typeof(OracleConnection))]
    [InlineData("MySql", "MySql", typeof(MySqlConnection))]
    public void CreateConnection_UsesConfiguredProvider(string registrationName, string provider, Type expectedType)
    {
        var factory = CreateFactory(registrationName, provider, enabled: true);

        using var connection = factory.CreateConnection(registrationName.ToLowerInvariant());

        Assert.IsType(expectedType, connection);
    }

    [Fact]
    public void CreateConnection_WhenRegistrationDisabled_Throws()
    {
        var factory = CreateFactory("Reporting", "SqlServer", enabled: false);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateConnection("Reporting"));

        Assert.Contains("disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCommandTimeoutSeconds_ReturnsConnectionSpecificValue()
    {
        var factory = CreateFactory("Reporting", "SqlServer", enabled: true, commandTimeoutSeconds: 120);

        var timeout = factory.GetCommandTimeoutSeconds("reporting");

        Assert.Equal(120, timeout);
    }

    private static DbConnectionFactory CreateFactory(string registrationName, string provider, bool enabled, int commandTimeoutSeconds = 30)
    {
        string connectionString;
        switch (provider)
        {
            case "Oracle":
                connectionString = "Data Source=localhost:1521/test;User Id=test;Password=test;";
                break;
            case "MySql":
                connectionString = "Server=localhost;Database=test;User ID=test;Password=test;";
                break;

            default:
                connectionString = "Server=localhost;Database=test;User ID=test;Password=test;TrustServerCertificate=True;";
                break;
        }

        var connectionOptions = new DatabaseConnectionOptions
        {
            Enabled = enabled,
            Provider = provider,
            ConnectionString = connectionString,
            CommandTimeoutSeconds = commandTimeoutSeconds
        };

        var connections = new Dictionary<string, DatabaseConnectionOptions>();
        connections.Add(registrationName, connectionOptions);

        var databaseOptions = new DatabaseOptions
        {
            Connections = connections
        };

        var options = Options.Create(databaseOptions);
        return new DbConnectionFactory(options, new TestConnectionStringProtector());
    }

    private sealed class TestConnectionStringProtector : IConnectionStringProtector
    {
        public string Encrypt(string connectionString)
        {
            return connectionString;
        }

        public string Decrypt(string protectedConnectionString)
        {
            return protectedConnectionString;
        }
    }
}
