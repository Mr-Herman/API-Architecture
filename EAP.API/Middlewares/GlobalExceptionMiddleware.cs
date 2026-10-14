using EAP.Application.Exceptions;
using EAP.Application.Localization;
using EAP.Api.Logging;
using EAP.Models.Constants;
using EAP.Models.Common;

namespace EAP.Api.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMessageLocalizer localizer)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException exception) when (context.RequestAborted.IsCancellationRequested)
        {
            RequestFailureContext.Set(context, "REQUEST_CANCELLED", exception, null, "Client");
        }
        catch (BusinessException exception)
        {
            var statusCode = GetStatusCode(exception);
            RequestFailureContext.Set(context, "BUSINESS_ERROR", exception, exception.Code);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(context, statusCode, exception.Message, exception.Code);
        }
        catch (Exception exception)
        {
            RequestFailureContext.Set(context, "UNHANDLED_ERROR", exception, BusinessCode.ServerError);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, localizer["InternalServerError"], BusinessCode.ServerError);
        }
    }

    private static int GetStatusCode(BusinessException exception)
    {
        if (exception is ResourceNotFoundException)
            return StatusCodes.Status404NotFound;
        if (exception is AuthenticationFailedException)
            return StatusCodes.Status401Unauthorized;

        return StatusCodes.Status400BadRequest;
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, int businessCode)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var result = ApiResult<object>.FailResult(message, businessCode);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }
}
