namespace EAP.DataAccess.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Databases";

    public Dictionary<string, DatabaseConnectionOptions> Connections { get; set; } = new();
}

public sealed class DatabaseConnectionOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 60;

    public bool HealthCheckEnabled { get; set; }
}
