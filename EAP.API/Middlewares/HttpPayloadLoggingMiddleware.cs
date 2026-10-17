using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Buffers;
using EAP.Api.LogViewer;
using EAP.Api.Logging;

namespace EAP.Api.Middlewares;

public sealed class HttpPayloadLoggingMiddleware
{
    private const string LoggerCategory = "EAP.Api.HttpPayload";

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger _payloadLogger;

    public HttpPayloadLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _payloadLogger = loggerFactory.CreateLogger(LoggerCategory);
    }

    public async Task InvokeAsync(HttpContext context, HttpPayloadSanitizer sanitizer)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var captureRequestBody = HasJsonBody(context.Request);
        if (captureRequestBody)
        {
            try
            {
                context.Request.EnableBuffering();
            }
            catch (Exception exception)
            {
                captureRequestBody = false;
                _payloadLogger.LogWarning(
                    "[HTTP_PAYLOAD_BUFFER_FAILED] TraceId={TraceId} Error={Error}",
                    context.TraceIdentifier,
                    exception.Message);
            }
        }

        var originalResponseBody = context.Response.Body;
        await using var responseCapture = new LimitedCaptureStream(
            originalResponseBody,
            LogViewerOptions.MaxPayloadBodyCharacters,
            () => IsJson(context.Response.ContentType));
        context.Response.Body = responseCapture;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
            string? requestBody = null;
            if (captureRequestBody)
            {
                try
                {
                    requestBody = await ReadRequestBodyAsync(
                        context.Request,
                        sanitizer,
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _payloadLogger.LogWarning(
                        "[HTTP_PAYLOAD_READ_FAILED] TraceId={TraceId} Error={Error}",
                        context.TraceIdentifier,
                        exception.Message);
                }
            }

            if (captureRequestBody || IsJson(context.Response.ContentType))
            {
                WritePayloadLog(context, requestBody, responseCapture, sanitizer);
            }
        }
    }

    private void WritePayloadLog(HttpContext context, string? requestBody, LimitedCaptureStream responseCapture, HttpPayloadSanitizer sanitizer)
    {
        try
        {
            var responseBody = IsJson(context.Response.ContentType)
                ? sanitizer.SanitizeJson(Encoding.UTF8.GetString(responseCapture.GetCapturedBytes()), responseCapture.LimitExceeded)
                : null;

            var record = new HttpPayloadLogRecord
            {
                LoggedAt = DateTime.Now,
                TraceId = context.TraceIdentifier,
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                StatusCode = context.Response.StatusCode,
                QueryParameters = sanitizer.SanitizeQuery(context.Request.Query),
                RequestBody = requestBody,
                ResponseBody = responseBody
            };

            _payloadLogger.LogInformation(
                "[HTTP_PAYLOAD] {Payload}",
                JsonSerializer.Serialize(record, PayloadJsonOptions));
        }
        catch (Exception exception)
        {
            _payloadLogger.LogWarning(
                "[HTTP_PAYLOAD_WRITE_FAILED] TraceId={TraceId} Error={Error}",
                context.TraceIdentifier,
                exception.Message);
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request, HttpPayloadSanitizer sanitizer, CancellationToken cancellationToken)
    {
        if (request.ContentLength is null or 0 || !IsJson(request.ContentType))
        {
            return null;
        }

        request.EnableBuffering();
        var originalPosition = request.Body.Position;
        request.Body.Position = 0;
        var buffer = ArrayPool<char>.Shared.Rent(LogViewerOptions.MaxPayloadBodyCharacters + 1);

        try
        {
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true);

            var totalRead = 0;
            while (totalRead <= LogViewerOptions.MaxPayloadBodyCharacters)
            {
                var read = await reader.ReadAsync(
                    buffer.AsMemory(
                        totalRead,
                        LogViewerOptions.MaxPayloadBodyCharacters + 1 - totalRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return sanitizer.SanitizeJson(
                new string(
                    buffer,
                    0,
                    Math.Min(totalRead, LogViewerOptions.MaxPayloadBodyCharacters)),
                totalRead > LogViewerOptions.MaxPayloadBodyCharacters);
        }
        finally
        {
            request.Body.Position = originalPosition;
            ArrayPool<char>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static bool HasJsonBody(HttpRequest request)
    {
        return request.ContentLength is > 0 && IsJson(request.ContentType);
    }

    private static bool IsJson(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("json",StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkip(PathString path)
    {
        return path.StartsWithSegments("/fis/Auth")
            || path.StartsWithSegments("/ops/logs")
            || path.StartsWithSegments("/log-viewer")
            || path.StartsWithSegments("/LogViewer")
            || path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/health")
            || path.Equals("/favicon.ico");
    }
}
