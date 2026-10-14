namespace EAP.Infrastructure.Options;

public sealed class HttpApiOptions
{
    public const string SectionName = "HttpApi";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public Dictionary<string, HttpApiEndpointOptions> Endpoints { get; set; } = [];
}

public sealed class HttpApiEndpointOptions
{
    public string Url { get; set; } = string.Empty;
    public string DefaultMethod { get; set; } = "POST";
    public bool RetryEnabled { get; set; }
}
