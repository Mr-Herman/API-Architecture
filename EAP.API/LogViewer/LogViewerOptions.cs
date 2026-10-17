namespace EAP.Api.LogViewer;

public sealed class LogViewerOptions
{
    public const string SectionName = "LogViewer";

    public const string LogDirectory = "logs";

    public const int PageSize = 50;

    public const int MaxPageNumber = 200;

    public const int MaxSearchDays = 7;

    public const int MaxScannedRecords = 50000;

    public const int MaxConcurrentQueries = 2;

    public const int QueryTimeoutSeconds = 5;

    public const int MaxRecordCharacters = 65536;

    public const int MaxPayloadBodyCharacters = 16 * 1024;

    public const int PayloadMatchWindowMinutes = 2;

    public bool Enabled { get; set; } = true;
}
