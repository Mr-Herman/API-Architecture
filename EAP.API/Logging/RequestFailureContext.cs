using System.Diagnostics;
using System.Reflection;

namespace EAP.Api.Logging;

public static class RequestFailureContext
{
    private static readonly object ItemKey = new();

    public static void Set(HttpContext context, string logType, Exception exception, int? errorCode, string? failureLocation = null)
    {
        var failure = new RequestFailureEntry(logType, exception, errorCode, failureLocation ?? FindFailureLocation(exception));

        context.Items[ItemKey] = failure;
    }

    public static RequestFailureEntry? Get(HttpContext context)
    {
        return context.Items.TryGetValue(ItemKey, out var value) ? value as RequestFailureEntry : null;
    }

    private static string FindFailureLocation(Exception exception)
    {
        var frames = new StackTrace(exception).GetFrames();
        if (frames is null)
        {
            return "Unknown";
        }

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            var declaringType = method?.DeclaringType;
            var namespaceName = declaringType?.Namespace ?? string.Empty;

            var layer = GetLayer(namespaceName);
            if (layer is null || method is null || declaringType is null)
            {
                continue;
            }

            var typeName = GetReadableTypeName(declaringType);
            var methodName = GetReadableMethodName(method, declaringType);
            return $"{layer}[{typeName}].{methodName}";
        }

        return "Unknown";
    }

    private static string? GetLayer(string namespaceName)
    {
        if (namespaceName.StartsWith("EAP.Api.Controllers", StringComparison.Ordinal))
        {
            return "Controller";
        }

        if (namespaceName.StartsWith("EAP.Application", StringComparison.Ordinal))
        {
            return "Application";
        }

        if (namespaceName.StartsWith("EAP.Infrastructure.Http", StringComparison.Ordinal))
        {
            return "ExternalHttp";
        }

        if (namespaceName.StartsWith("EAP.DataAccess.Repositories", StringComparison.Ordinal)
            || namespaceName.StartsWith("EAP.DataAccess.Database", StringComparison.Ordinal)
            || namespaceName.StartsWith("EAP.DataAccess.Base", StringComparison.Ordinal))
        {
            return "Database";
        }

        if (namespaceName.StartsWith("EAP.Infrastructure.Security", StringComparison.Ordinal))
        {
            return "Security";
        }

        if (namespaceName.StartsWith("EAP.DataAccess.Health", StringComparison.Ordinal))
        {
            return "HealthCheck";
        }

        if (namespaceName.StartsWith("EAP.Infrastructure", StringComparison.Ordinal))
        {
            return "Infrastructure";
        }

        return null;
    }

    private static string GetReadableTypeName(Type declaringType)
    {
        var type = declaringType;
        while (type.DeclaringType is not null)
        {
            type = type.DeclaringType;
        }

        return type.Name;
    }

    private static string GetReadableMethodName(MethodBase method, Type declaringType)
    {
        if (method.Name != "MoveNext")
        {
            return method.Name;
        }

        var generatedName = declaringType.Name;
        var start = generatedName.IndexOf('<');
        var end = generatedName.IndexOf('>');
        if (start >= 0 && end > start)
        {
            return generatedName[(start + 1)..end];
        }

        return method.Name;
    }
}

public sealed class RequestFailureEntry
{
    public RequestFailureEntry(string logType, Exception exception, int? errorCode, string failureLocation)
    {
        LogType = logType;
        Exception = exception;
        ErrorCode = errorCode;
        FailureLocation = failureLocation;
    }

    public string LogType { get; }

    public Exception Exception { get; }

    public int? ErrorCode { get; }

    public string FailureLocation { get; }
}
