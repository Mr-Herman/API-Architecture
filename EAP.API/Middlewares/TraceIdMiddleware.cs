using EAP.Models.Constants;

namespace EAP.Api.Middlewares;

public sealed class TraceIdMiddleware
{
    private const int MaxTraceIdLength = 128;

    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incomingTraceId = context.Request.Headers[HttpHeaderConst.XTraceId].ToString();

        if (IsValidTraceId(incomingTraceId))
        {
            context.TraceIdentifier = incomingTraceId;
        }
        context.Response.OnStarting(AddTraceIdResponseHeader, context);
        await _next(context);
    }

    private static bool IsValidTraceId(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return false;
        }
        return traceId.Length <= MaxTraceIdLength;
    }

    private static Task AddTraceIdResponseHeader(object state)
    {
        var context = (HttpContext)state;
        context.Response.Headers[HttpHeaderConst.XTraceId] = context.TraceIdentifier;
        return Task.CompletedTask;
    }
}
