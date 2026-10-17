namespace EAP.Api.LogViewer;

public interface ILogFileReader
{
    Task<LogQueryPage?> TryQueryAsync(LogQueryRequest request, CancellationToken cancellationToken);
}
