namespace EAP.Api.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int QueueLimit { get; set; }

    public EapClientRateLimitOptions EapClient { get; set; } = new();

    public TokenRateLimitOptions Token { get; set; } = new();
}

public sealed class EapClientRateLimitOptions
{
    public bool Enabled { get; set; } = true;

    public int DefaultIntervalSeconds { get; set; } = 1;

    public int ObservationWindowSeconds { get; set; } = 30;

    public int RequestThreshold { get; set; } = 30;

    public int CooldownSeconds { get; set; } = 30;

    public int ViolationPeriodSeconds { get; set; } = 120;

    public int ViolationWindowsBeforeBan { get; set; } = 3;

    public int BanSeconds { get; set; } = 300;

    public int StateExpirationMinutes { get; set; } = 15;

    public Dictionary<string, int> RouteIntervalSeconds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TokenRateLimitOptions
{
    public int PermitLimit { get; set; } = 3;

    public int WindowSeconds { get; set; } = 60;

    public int BanSeconds { get; set; } = 300;

    public int SystemTokenLimit { get; set; } = 200;

    public int SystemTokensPerPeriod { get; set; } = 100;

    public int SystemReplenishmentSeconds { get; set; } = 1;
}
