using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace EAP.Api.Logging;

internal static class TechnicalRequestFilter
{
    private static readonly HashSet<string> StaticFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".eot",
        ".gif",
        ".ico",
        ".jpeg",
        ".jpg",
        ".js",
        ".map",
        ".png",
        ".svg",
        ".ttf",
        ".webp",
        ".woff",
        ".woff2"
    };

    public static bool ShouldSkipRequestLog(HttpContext context)
    {
        if (IsAlwaysIgnored(context.Request))
        {
            return true;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is null)
        {
            return true;
        }

        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return false;
        }

        return HttpMethods.IsOptions(context.Request.Method)
            || IsDocumentationOrHealthPath(context.Request.Path)
            || IsStaticFile(context.Request.Path);
    }

    public static bool ShouldSkipPayloadLog(HttpRequest request)
    {
        return IsAlwaysIgnored(request)
            || HttpMethods.IsOptions(request.Method)
            || request.Path.StartsWithSegments("/fis/Auth")
            || IsDocumentationOrHealthPath(request.Path)
            || IsStaticFile(request.Path);
    }

    public static bool ShouldSkipLogViewerEntry(string? method, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var requestMethod = method ?? string.Empty;
        var requestPath = new PathString(path);
        return IsAlwaysIgnored(requestMethod, requestPath)
            || HttpMethods.IsOptions(requestMethod)
            || IsDocumentationOrHealthPath(requestPath)
            || IsStaticFile(requestPath);
    }

    private static bool IsAlwaysIgnored(HttpRequest request)
    {
        return IsAlwaysIgnored(request.Method, request.Path);
    }

    private static bool IsAlwaysIgnored(string method, PathString path)
    {
        return HttpMethods.IsHead(method)
            || path.Equals("/")
            || path.StartsWithSegments("/ops/logs")
            || path.StartsWithSegments("/log-viewer")
            || path.StartsWithSegments("/LogViewer")
            || IsBrowserMetadataPath(path);
    }

    private static bool IsDocumentationOrHealthPath(PathString path)
    {
        return path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/health");
    }

    private static bool IsBrowserMetadataPath(PathString path)
    {
        return path.Equals("/favicon.ico")
            || path.Equals("/robots.txt")
            || path.Equals("/sitemap.xml")
            || path.Equals("/site.webmanifest")
            || path.Equals("/browserconfig.xml")
            || path.Equals("/apple-touch-icon.png")
            || path.Equals("/apple-touch-icon-precomposed.png")
            || path.Equals("/.well-known/appspecific/com.chrome.devtools.json");
    }

    private static bool IsStaticFile(PathString path)
    {
        var extension = Path.GetExtension(path.Value);
        return !string.IsNullOrEmpty(extension)
            && StaticFileExtensions.Contains(extension);
    }
}
