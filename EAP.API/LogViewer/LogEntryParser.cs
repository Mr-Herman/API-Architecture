using System.Globalization;
using System.Text.RegularExpressions;

namespace EAP.Api.LogViewer;

public sealed partial class LogEntryParser
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public bool IsLogEntryStart(string line)
    {
        return HeaderPattern().IsMatch(line);
    }

    public LogEntryResponse? Parse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var firstLineEnd = rawText.IndexOf('\n');
        var firstLine = firstLineEnd >= 0 ? rawText[..firstLineEnd].TrimEnd('\r') : rawText;
        var header = HeaderPattern().Match(firstLine);
        if (!header.Success
            || !DateTime.TryParseExact(
                header.Groups["timestamp"].Value,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            return null;
        }

        var message = header.Groups["message"].Value;
        var request = RequestPattern().Match(message);
        var timingSteps = ParseTimingSteps(message);

        return new LogEntryResponse
        {
            Timestamp = timestamp,
            Level = header.Groups["level"].Value,
            LogType = ReadValue(LogTypePattern(), message) ?? "LOG",
            HttpMethod = request.Success ? request.Groups["method"].Value : null,
            Path = request.Success ? request.Groups["path"].Value : null,
            StatusCode = ReadInt(StatusPattern(), message),
            TotalMilliseconds = ReadDouble(TotalPattern(), message),
            TraceId = ReadValue(TracePattern(), firstLine),
            FailureLocation = ReadValue(FailedAtPattern(), message),
            ErrorCode = ReadInt(CodePattern(), message),
            Message = message,
            RawText = rawText,
            TimingSteps = timingSteps
        };
    }

    private static IReadOnlyList<LogTimingStepResponse> ParseTimingSteps(string message)
    {
        var results = new List<LogTimingStepResponse>();
        foreach (Match match in TimingPattern().Matches(message))
        {
            if (!double.TryParse(
                match.Groups["elapsed"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var elapsed))
            {
                continue;
            }

            results.Add(new LogTimingStepResponse
            {
                Layer = match.Groups["layer"].Value,
                Resource = match.Groups["resource"].Value,
                Method = match.Groups["method"].Value,
                ElapsedMilliseconds = elapsed
            });
        }

        return results;
    }

    private static string? ReadValue(Regex pattern, string source)
    {
        var match = pattern.Match(source);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static int? ReadInt(Regex pattern, string source)
    {
        var value = ReadValue(pattern, source);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static double? ReadDouble(Regex pattern, string source)
    {
        var value = ReadValue(pattern, source);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [GeneratedRegex("^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3}) \\| (?<level>[A-Z]+) \\| (?<message>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex HeaderPattern();

    [GeneratedRegex("\\[(?<value>[A-Z_]+)\\]", RegexOptions.CultureInvariant)]
    private static partial Regex LogTypePattern();

    [GeneratedRegex("(?:^|\\] )(?<method>GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS) (?<path>/[^ |?]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex RequestPattern();

    [GeneratedRegex("Status=(?<value>\\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StatusPattern();

    [GeneratedRegex("Total=(?<value>\\d+(?:\\.\\d+)?)ms", RegexOptions.CultureInvariant)]
    private static partial Regex TotalPattern();

    [GeneratedRegex("Trace=(?<value>[^|\\r\\n]+)", RegexOptions.CultureInvariant)]
    private static partial Regex TracePattern();

    [GeneratedRegex("FailedAt=(?<value>[^|\\r\\n]+)", RegexOptions.CultureInvariant)]
    private static partial Regex FailedAtPattern();

    [GeneratedRegex("Code=(?<value>-?\\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("(?<layer>Controller|Application|Database)\\[(?<resource>[^\\]]+)\\]\\.(?<method>[^= >|]+)=(?<elapsed>\\d+(?:\\.\\d+)?)ms", RegexOptions.CultureInvariant)]
    private static partial Regex TimingPattern();
}
