using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Api.Extensions;
using EAP.Api.RateLimiting;
using EAP.Models.Common;
using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EAP.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("fis/Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IMessageLocalizer _localizer;
    private readonly IEapClientRequestProtection _requestProtection;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ITokenService tokenService, IMessageLocalizer localizer, IEapClientRequestProtection requestProtection, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _localizer = localizer;
        _requestProtection = requestProtection;
        _logger = logger;
    }

    [HttpPost("token")]
    [EnableRateLimiting(RateLimitingExtensions.TokenPolicyName)]
    public async Task<ApiResult<TokenResponse>> CreateToken([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateTokenAsync(request.ClientId, request.ClientSecret, cancellationToken);
        if (token is null)
            throw new AuthenticationFailedException(_localizer["InvalidCredentials"]);

        var decision = _requestProtection.CheckTokenRequest(request.ClientId);
        if (!decision.Allowed)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();

            if (decision.ShouldLog)
            {
                _logger.LogWarning(
                    "[TOKEN_RATE_LIMITED] EapClientId={EapClientId} RetryAfterSeconds={RetryAfterSeconds}",
                    request.ClientId,
                    decision.RetryAfterSeconds);
            }

            return ApiResult<TokenResponse>.FailResult(_localizer["TokenRequestTooFrequent"]);
        }

        return ApiResult<TokenResponse>.SuccessResult(token, _localizer["Success"]);
    }
}
