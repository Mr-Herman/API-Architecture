namespace EAP.Api.Logging;

public sealed class HttpPayloadLogRecord
{
    public DateTime LoggedAt { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string? QueryParameters { get; set; }

    public string? RequestBody { get; set; }

    public string? ResponseBody { get; set; }
}
