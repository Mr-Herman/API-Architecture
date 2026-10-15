namespace EAP.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public string SigningKey { get; set; } = string.Empty;
    public int ClockSkewSeconds { get; set; } = 30;
    public List<JwtClientOptions> Clients { get; set; } = [];
}

public sealed class JwtClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public bool AcceptEapClientIds { get; set; }
}
