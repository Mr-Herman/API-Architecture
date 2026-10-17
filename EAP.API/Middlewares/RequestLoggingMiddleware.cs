using System.Diagnostics;
using System.Text;
using EAP.Api.Logging;
using EAP.Application.Logging;
using EAP.Models.Constants;

namespace EAP.Api.Middlewares;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var requestActivity = Activity.Current;

        var requestId = context.Request.Headers[HttpHeaderConst.XRequestId].ToString();
        var loggingProperties = new Dictionary<string, object?>
        {
            { "TraceId", context.TraceIdentifier },
            { "RequestId", requestId }
        };

        using (_logger.BeginScope(loggingProperties))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                if (!ShouldSkipTechnicalRequest(context))
                {
                    WriteRequestSummary(context, requestActivity, elapsed);
                }
            }
        }
    }

    private void WriteRequestSummary(HttpContext context, Activity? requestActivity, TimeSpan elapsed)
    {
        var timingChain = BuildTimingChain(requestActivity);
        var failure = RequestFailureContext.Get(context);

        if (failure is not null)
        {
            WriteFailureSummary(context, timingChain, elapsed, failure);
            return;
        }

        var failed = context.Response.StatusCode >= StatusCodes.Status400BadRequest;
        var logType = failed ? "[REQUEST_FAILED]" : "[REQUEST]";
        const string message =
            "{LogType} {HttpMethod} {Path} | Status={StatusCode} | Total={TotalMs:F1}ms | {TimingChain}";

        if (failed)
        {
            _logger.LogWarning(
                message,
                logType,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                timingChain);
            return;
        }

        _logger.LogInformation(
            message,
            logType,
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds,
            timingChain);
    }

    private void WriteFailureSummary(HttpContext context, string timingChain, TimeSpan elapsed, RequestFailureEntry failure)
    {
        if (failure.LogType == "REQUEST_CANCELLED")
        {
            _logger.LogInformation(
                "[REQUEST_CANCELLED] {HttpMethod} {Path} | Total={TotalMs:F1}ms | {TimingChain}",
                context.Request.Method,
                context.Request.Path.Value,
                elapsed.TotalMilliseconds,
                timingChain);
            return;
        }

        const string message =
            "[{LogType}] {HttpMethod} {Path} | Status={StatusCode} | Total={TotalMs:F1}ms | {TimingChain} | FailedAt={FailedAt} | Code={ErrorCode} | Error={ErrorMessage}";

        var arguments = new object?[]
        {
            failure.LogType,
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            elapsed.TotalMilliseconds,
            timingChain,
            failure.FailureLocation,
            failure.ErrorCode,
            failure.Exception.Message
        };

        if (failure.LogType == "UNHANDLED_ERROR")
        {
            _logger.LogError(failure.Exception, message, arguments);
            return;
        }

        _logger.LogWarning(message, arguments);
    }

    private static string BuildTimingChain(Activity? requestActivity)
    {
        var timings = MethodTimingScope.GetTimings(requestActivity);
        if (timings.Count == 0)
        {
            return "Pipeline";
        }

        var builder = new StringBuilder();
        foreach (var timing in timings)
        {
            if (builder.Length > 0)
            {
                builder.Append(" > ");
            }

            builder.Append(timing.Layer);
            if (!string.IsNullOrWhiteSpace(timing.ResourceName))
            {
                builder.Append('[');
                builder.Append(timing.ResourceName);
                builder.Append(']');
            }

            builder.Append('.');
            builder.Append(timing.MethodName);
            builder.Append('=');
            builder.Append(timing.ElapsedMilliseconds.ToString("F1"));
            builder.Append("ms");
        }

        return builder.ToString();
    }

    private static bool ShouldSkipTechnicalRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/ops/logs")
            || context.Request.Path.StartsWithSegments("/log-viewer")
            || context.Request.Path.StartsWithSegments("/LogViewer")
            || context.Request.Path.Equals("/favicon.ico"))
        {
            return true;
        }

        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments("/swagger")
            || context.Request.Path.StartsWithSegments("/health");
    }
}
