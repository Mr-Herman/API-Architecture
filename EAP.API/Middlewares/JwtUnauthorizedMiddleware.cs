using EAP.Application.Localization;
using EAP.Models.Constants;
using EAP.Models.Common;
using EAP.Models.Resources;
using Microsoft.Extensions.Localization;

namespace EAP.Api.Middlewares;

public sealed class JwtUnauthorizedMiddleware
{
    private readonly RequestDelegate _next;

    public JwtUnauthorizedMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMessageLocalizer localizer)
    {
        await _next(context);
        if (context.Response.HasStarted)
        {
            return;
        }
        ApiResult<object> result;
        switch (context.Response.StatusCode)
        {
            case StatusCodes.Status401Unauthorized:
                result = ApiResult<object>.FailResult(localizer["Unauthorized"], BusinessCode.JwtInvalid);
                break;
            case StatusCodes.Status403Forbidden:
                result = ApiResult<object>.FailResult(localizer["Forbidden"], BusinessCode.PermissionDenied);
                break;
            default:
                return;
        }
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }
}
