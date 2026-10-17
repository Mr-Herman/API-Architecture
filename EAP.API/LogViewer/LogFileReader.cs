using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using EAP.Api.Logging;

namespace EAP.Api.LogViewer;

public sealed class LogFileReader : ILogFileReader, IDisposable
{
    private const int ReadBufferSize = 64 * 1024;
    private readonly LogEntryParser _parser;
    private readonly SemaphoreSlim _queryGate;
    private readonly string _logDirectory;

    public LogFileReader(LogEntryParser parser) : this(parser, ResolveLogDirectory(LogViewerOptions.LogDirectory))
    {
    }

    internal LogFileReader(LogEntryParser parser, string logDirectory)
    {
        _parser = parser;
        _queryGate = new SemaphoreSlim(
            LogViewerOptions.MaxConcurrentQueries,
            LogViewerOptions.MaxConcurrentQueries);
        _logDirectory = Path.GetFullPath(logDirectory);
    }

    public async Task<LogQueryPage?> TryQueryAsync(LogQueryRequest request, CancellationToken cancellationToken)
    {
        if (!await _queryGate.WaitAsync(0, cancellationToken))
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(LogViewerOptions.QueryTimeoutSeconds));
            return await QueryCoreAsync(request, timeout.Token, cancellationToken);
        }
        finally
        {
            _queryGate.Release();
        }
    }

    public void Dispose()
    {
        _queryGate.Dispose();
    }

    private async Task<LogQueryPage> QueryCoreAsync(LogQueryRequest request, CancellationToken queryToken, CancellationToken requestToken)
    {
        var endDate = request.EndDate ?? request.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        var startDate = request.StartDate ?? endDate;
        var pageSize = LogViewerOptions.PageSize;
        var page = Math.Min(request.Page, LogViewerOptions.MaxPageNumber);
        var offset = (page - 1) * pageSize;
        var requiredMatches = offset + pageSize + 1;
        var matches = new List<LogEntryResponse>(requiredMatches * 2);
        var scannedRecords = 0;
        var truncated = false;

        try
        {
            for (var date = endDate; date >= startDate; date = date.AddDays(-1))
            {
                foreach (var file in FindLogFiles(date, request.Category))
                {
                    var fileMatches = 0;
                    await foreach (var entry in ReadEntriesReverseAsync(file, queryToken))
                    {
                        scannedRecords++;
                        if (Matches(entry, request))
                        {
                            matches.Add(entry);
                            fileMatches++;
                        }

                        if (scannedRecords >= LogViewerOptions.MaxScannedRecords)
                        {
                            truncated = true;
                            break;
                        }

                        if (fileMatches >= requiredMatches)
                        {
                            break;
                        }
                    }

                    if (truncated)
                    {
                        break;
                    }
                }

                if (truncated)
                {
                    break;
                }

                if (matches.Count >= requiredMatches)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (!requestToken.IsCancellationRequested)
        {
            truncated = true;
        }

        var ordered = matches
            .OrderByDescending(entry => entry.Timestamp)
            .Take(requiredMatches)
            .ToArray();
        var items = ordered.Skip(offset).Take(pageSize).ToArray();

        try
        {
            await AttachPayloadsAsync(items, queryToken);
        }
        catch (OperationCanceledException) when (!requestToken.IsCancellationRequested)
        {
            truncated = true;
        }

        return new LogQueryPage
        {
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize,
            HasPrevious = page > 1,
            HasNext = ordered.Length > offset + pageSize || truncated,
            Truncated = truncated,
            Items = items
        };
    }

    private async Task AttachPayloadsAsync(IReadOnlyList<LogEntryResponse> entries, CancellationToken cancellationToken)
    {
        var entriesByTraceId = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.TraceId))
            .GroupBy(entry => entry.TraceId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        if (entriesByTraceId.Count == 0)
        {
            return;
        }

        var unresolvedTraceIds = new HashSet<string>(entriesByTraceId.Keys, StringComparer.OrdinalIgnoreCase);
        var dates = entries.Select(entry => DateOnly.FromDateTime(entry.Timestamp)).Distinct().OrderByDescending(date => date);
        var scannedRecords = 0;

        foreach (var date in dates)
        {
            foreach (var file in FindPayloadFiles(date))
            {
                await foreach (var line in ReadLinesReverseAsync(file, cancellationToken))
                {
                    scannedRecords++;
                    var payload = ParsePayload(line);
                    if (payload is not null && unresolvedTraceIds.Contains(payload.TraceId))
                    {
                        var matchedEntries = entriesByTraceId[payload.TraceId]
                            .Where(entry => IsSameRequest(entry, payload))
                            .ToArray();

                        foreach (var entry in matchedEntries)
                        {
                            entry.QueryParameters = payload.QueryParameters;
                            entry.RequestBody = payload.RequestBody;
                            entry.ResponseBody = payload.ResponseBody;
                        }

                        if (matchedEntries.Length > 0)
                        {
                            unresolvedTraceIds.Remove(payload.TraceId);
                        }
                    }

                    if (unresolvedTraceIds.Count == 0 || scannedRecords >= LogViewerOptions.MaxScannedRecords)
                    {
                        return;
                    }
                }
            }
        }
    }

    private static bool IsSameRequest(LogEntryResponse entry, HttpPayloadLogRecord payload)
    {
        return string.Equals(entry.TraceId, payload.TraceId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.HttpMethod, payload.Method, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.Path, payload.Path, StringComparison.OrdinalIgnoreCase)
            && entry.StatusCode == payload.StatusCode
            && Math.Abs((entry.Timestamp - payload.LoggedAt).TotalMinutes)
                <= LogViewerOptions.PayloadMatchWindowMinutes;
    }

    private IEnumerable<string> FindPayloadFiles(DateOnly date)
    {
        var payloadDirectory = Path.Combine(_logDirectory, "payload");
        if (!Directory.Exists(payloadDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(
                payloadDirectory,
                $"eap-api-payload-{date:yyyy-MM-dd}*.log",
                SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
    }

    private static HttpPayloadLogRecord? ParsePayload(string line)
    {
        const string marker = "[HTTP_PAYLOAD] ";
        const string legacyMarker = "[HTTP_ERROR_PAYLOAD] ";
        var markerIndex = line.IndexOf(marker, StringComparison.Ordinal);
        var markerLength = marker.Length;

        if (markerIndex < 0)
        {
            markerIndex = line.IndexOf(legacyMarker, StringComparison.Ordinal);
            markerLength = legacyMarker.Length;
        }

        if (markerIndex < 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<HttpPayloadLogRecord>(line[(markerIndex + markerLength)..]);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private IEnumerable<string> FindLogFiles(DateOnly date, LogCategory category)
    {
        if (!Directory.Exists(_logDirectory))
        {
            return Array.Empty<string>();
        }

        var dateText = date.ToString("yyyy-MM-dd");
        var files = new List<string>();

        if (category is LogCategory.All or LogCategory.Normal)
        {
            files.AddRange(Directory.EnumerateFiles(
                _logDirectory,
                $"eap-api-{dateText}*.log",
                SearchOption.TopDirectoryOnly));
        }

        if (category is LogCategory.All or LogCategory.Error)
        {
            var errorDirectory = Path.Combine(_logDirectory, "error");
            if (Directory.Exists(errorDirectory))
            {
                files.AddRange(Directory.EnumerateFiles(
                    errorDirectory,
                    $"eap-api-error-{dateText}*.log",
                    SearchOption.TopDirectoryOnly));
            }
        }

        return files.OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
    }

    private async IAsyncEnumerable<LogEntryResponse> ReadEntriesReverseAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var continuationLines = new List<string>();
        var continuationLength = 0;

        await foreach (var line in ReadLinesReverseAsync(filePath, cancellationToken))
        {
            if (!_parser.IsLogEntryStart(line))
            {
                if (continuationLength < LogViewerOptions.MaxRecordCharacters)
                {
                    continuationLines.Add(line);
                    continuationLength += line.Length + Environment.NewLine.Length;
                }

                continue;
            }

            var rawText = BuildRawText(line, continuationLines);
            var entry = _parser.Parse(rawText);
            continuationLines.Clear();
            continuationLength = 0;

            if (entry is not null)
            {
                yield return entry;
            }
        }
    }

    private static string BuildRawText(string header, List<string> continuationLines)
    {
        if (continuationLines.Count == 0)
        {
            return header;
        }

        continuationLines.Reverse();
        return header + Environment.NewLine + string.Join(Environment.NewLine, continuationLines);
    }

    private static async IAsyncEnumerable<string> ReadLinesReverseAsync(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            ReadBufferSize,
            FileOptions.Asynchronous | FileOptions.RandomAccess);

        var rented = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        var remainder = Array.Empty<byte>();

        try
        {
            var position = stream.Length;
            while (position > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesToRead = (int)Math.Min(ReadBufferSize, position);
                position -= bytesToRead;
                stream.Position = position;

                var bytesRead = 0;
                while (bytesRead < bytesToRead)
                {
                    var read = await stream.ReadAsync(
                        rented.AsMemory(bytesRead, bytesToRead - bytesRead),
                        cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    bytesRead += read;
                }

                var combined = new byte[bytesRead + remainder.Length];
                Buffer.BlockCopy(rented, 0, combined, 0, bytesRead);
                Buffer.BlockCopy(remainder, 0, combined, bytesRead, remainder.Length);

                var lineEnd = combined.Length;
                for (var index = combined.Length - 1; index >= 0; index--)
                {
                    if (combined[index] != (byte)'\n')
                    {
                        continue;
                    }

                    var lineStart = index + 1;
                    if (lineEnd > lineStart)
                    {
                        yield return DecodeLine(combined, lineStart, lineEnd - lineStart);
                    }

                    lineEnd = index;
                }

                remainder = combined[..lineEnd];
            }

            if (remainder.Length > 0)
            {
                yield return DecodeLine(remainder, 0, remainder.Length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string DecodeLine(byte[] bytes, int index, int count)
    {
        if (count > 0 && bytes[index + count - 1] == (byte)'\r')
        {
            count--;
        }

        return Encoding.UTF8.GetString(bytes, index, count);
    }

    private static bool Matches(LogEntryResponse entry, LogQueryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Level)
            && !string.Equals(entry.Level, request.Level.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.TraceId)
            && (entry.TraceId is null
                || !entry.TraceId.Contains(request.TraceId.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(request.Keyword)
            || entry.RawText.Contains(request.Keyword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveLogDirectory(string configuredDirectory)
    {
        return Path.IsPathRooted(configuredDirectory)
            ? Path.GetFullPath(configuredDirectory)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDirectory));
    }
}
