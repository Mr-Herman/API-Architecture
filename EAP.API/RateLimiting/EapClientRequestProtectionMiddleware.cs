using System.Security.Claims;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace EAP.Api.RateLimiting;

public sealed class EapClientRequestProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EapClientRequestProtectionMiddleware> _logger;

    public EapClientRequestProtectionMiddleware(RequestDelegate next, ILogger<EapClientRequestProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IEapClientRequestProtection protectionService, IMessageLocalizer localizer)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null
            || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var eapClientId = GetEapClientId(context.User);
        if (string.IsNullOrWhiteSpace(eapClientId))
        {
            _logger.LogWarning(
                "[REQUEST_PROTECTION] Authenticated request rejected because EapClientId is missing. Path={Path}",
                context.Request.Path.Value);
            await WriteMissingClientIdResponseAsync(context, localizer);
            return;
        }

        var endpointKey = CreateEndpointKey(context, endpoint);
        var decision = protectionService.CheckBusinessRequest(eapClientId, endpointKey);
        if (decision.Allowed)
        {
            await _next(context);
            return;
        }

        if (decision.ShouldLog)
        {
            _logger.LogWarning(
                "[REQUEST_PROTECTION] EapClientId={EapClientId} Endpoint={Endpoint} Reason={Reason} RetryAfterSeconds={RetryAfterSeconds}",
                eapClientId,
                endpointKey,
                decision.Reason,
                decision.RetryAfterSeconds);
        }

        await WriteRejectedResponseAsync(context, localizer, decision);
    }

    private static string? GetEapClientId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtClaimNames.EapClientId);
    }

    private static string CreateEndpointKey(HttpContext context, Endpoint endpoint)
    {
        var route = endpoint is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText
            : context.Request.Path.Value;

        return $"{context.Request.Method} {route ?? "unknown"}";
    }

    private static async Task WriteRejectedResponseAsync(HttpContext context, IMessageLocalizer localizer, RequestLimitDecision decision)
    {
        var messageKey = decision.Reason switch
        {
            RequestLimitReason.EapClientBlocked => "EapClientTemporarilyBlocked",
            RequestLimitReason.AbnormalWindow => "EapClientRequestAbnormal",
            _ => "TooManyRequests"
        };

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();

        var result = ApiResult<object>.FailResult(localizer[messageKey], 4007);
        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private static async Task WriteMissingClientIdResponseAsync(HttpContext context, IMessageLocalizer localizer)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object>.FailResult(localizer["Forbidden"], 4007);
        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }
}
