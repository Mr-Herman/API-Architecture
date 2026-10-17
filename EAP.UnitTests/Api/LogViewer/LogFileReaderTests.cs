using EAP.Api.LogViewer;

namespace EAP.UnitTests.Api.LogViewer;

public sealed class LogFileReaderTests : IDisposable
{
    private readonly string _directory;

    public LogFileReaderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "eap-log-viewer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task TryQueryAsync_ReturnsNewestMatchingEntriesFirstAndPagesResults()
    {
        var date = new DateOnly(2026, 10, 17);
        var started = new DateTime(2026, 10, 17, 9, 0, 0);
        var log = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 51).Select(index =>
                $"{started.AddMinutes(index):yyyy-MM-dd HH:mm:ss.fff} | INFO | [REQUEST] GET /api/item-{index:00} | Status=200 | Total=10.0ms | Pipeline | Trace=trace-{index:00}"));
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "eap-api-2026-10-17.log"),
            log + Environment.NewLine);

        using var reader = CreateReader();
        var page = await reader.TryQueryAsync(
            new LogQueryRequest
            {
                StartDate = date,
                EndDate = date,
                Page = 1
            },
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(LogViewerOptions.PageSize, page.Items.Count);
        Assert.Equal("/api/item-50", page.Items[0].Path);
        Assert.Equal("/api/item-01", page.Items[^1].Path);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task TryQueryAsync_FiltersByLevelAndKeyword()
    {
        var date = new DateOnly(2026, 10, 17);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "eap-api-2026-10-17.log"),
            "2026-10-17 10:00:00.000 | WARN | [REQUEST_FAILED] POST /fis/Production/TrackOut | Status=400 | Total=20.0ms | Pipeline | Trace=trace-02" + Environment.NewLine);

        using var reader = CreateReader();
        var page = await reader.TryQueryAsync(
            new LogQueryRequest
            {
                StartDate = date,
                EndDate = date,
                Level = "WARN",
                Keyword = "TrackOut"
            },
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("WARN", page.Items[0].Level);
    }

    [Fact]
    public async Task TryQueryAsync_AttachesSuccessfulPayloadByTraceId()
    {
        var date = new DateOnly(2026, 10, 17);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "eap-api-2026-10-17.log"),
            "2026-10-17 10:00:00.000 | INFO | [REQUEST] POST /fis/test | Status=200 | Total=20.0ms | Pipeline | Trace=trace-payload" + Environment.NewLine);

        var payloadDirectory = Path.Combine(_directory, "payload");
        Directory.CreateDirectory(payloadDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(payloadDirectory, "eap-api-payload-2026-10-17.log"),
            "2026-10-17 10:00:00.001 | [HTTP_PAYLOAD] {\"LoggedAt\":\"2026-10-17T10:00:00.001\",\"TraceId\":\"trace-payload\",\"Method\":\"POST\",\"Path\":\"/fis/test\",\"StatusCode\":200,\"RequestBody\":\"{\\\"LotId\\\":\\\"LOT-1\\\"}\",\"ResponseBody\":\"{\\\"Status\\\":1}\"}" + Environment.NewLine);

        using var reader = CreateReader();
        var page = await reader.TryQueryAsync(
            new LogQueryRequest { StartDate = date, EndDate = date },
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Contains("LOT-1", page.Items[0].RequestBody);
        Assert.Contains("Status", page.Items[0].ResponseBody);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private LogFileReader CreateReader()
    {
        return new LogFileReader(new LogEntryParser(), _directory);
    }
}
