using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("fis/Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IMessageLocalizer _localizer;

    public AuthController(ITokenService tokenService, IMessageLocalizer localizer)
    {
        _tokenService = tokenService;
        _localizer = localizer;
    }

    [HttpPost("token")]
    public async Task<ApiResult<TokenResponse>> CreateToken([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateTokenAsync(request.ClientId, request.ClientSecret, cancellationToken);
        if (token is null)
            throw new AuthenticationFailedException(_localizer["InvalidCredentials"]);

        return ApiResult<TokenResponse>.SuccessResult(token, _localizer["Success"]);
    }
}
