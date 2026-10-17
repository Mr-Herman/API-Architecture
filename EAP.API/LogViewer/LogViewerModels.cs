using System.ComponentModel.DataAnnotations;

namespace EAP.Api.LogViewer;

public enum LogCategory
{
    All,
    Normal,
    Error
}

public sealed class LogViewerSettingsResponse
{
    public int PageSize { get; set; }

    public int MaxSearchDays { get; set; }
}

public sealed class LogQueryRequest
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public LogCategory Category { get; set; } = LogCategory.All;

    [StringLength(10)]
    public string? Level { get; set; }

    [StringLength(200)]
    public string? Keyword { get; set; }

    [StringLength(200)]
    public string? TraceId { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

}

public sealed class LogQueryPage
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public bool HasPrevious { get; set; }

    public bool HasNext { get; set; }

    public bool Truncated { get; set; }

    public IReadOnlyList<LogEntryResponse> Items { get; set; } = Array.Empty<LogEntryResponse>();
}

public sealed class LogEntryResponse
{
    public DateTime Timestamp { get; set; }

    public string Level { get; set; } = string.Empty;

    public string LogType { get; set; } = string.Empty;

    public string? HttpMethod { get; set; }

    public string? Path { get; set; }

    public int? StatusCode { get; set; }

    public double? TotalMilliseconds { get; set; }

    public string? TraceId { get; set; }

    public string? FailureLocation { get; set; }

    public int? ErrorCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public string? QueryParameters { get; set; }

    public string? RequestBody { get; set; }

    public string? ResponseBody { get; set; }

    public IReadOnlyList<LogTimingStepResponse> TimingSteps { get; set; } = Array.Empty<LogTimingStepResponse>();
}

public sealed class LogTimingStepResponse
{
    public string Layer { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public double ElapsedMilliseconds { get; set; }
}
