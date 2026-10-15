using EAP.Api.Controllers;
using EAP.Api.RateLimiting;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task CreateToken_ForwardsCredentialsAndReturnsServiceResult()
    {
        var tokenService = new Mock<ITokenService>();
        var localizer = new Mock<IMessageLocalizer>();
        var protection = new Mock<IEapClientRequestProtection>();
        var request = new TokenRequest { ClientId = "client", ClientSecret = "secret" };
        tokenService.Setup(x => x.CreateTokenAsync("client", "secret", It.IsAny<CancellationToken>())).ReturnsAsync((TokenResponse?)null);
        var controller = CreateController(tokenService.Object, localizer.Object, protection.Object);

        await Assert.ThrowsAsync<EAP.Application.Exceptions.AuthenticationFailedException>(() => controller.CreateToken(request, CancellationToken.None));
        tokenService.Verify(x => x.CreateTokenAsync("client", "secret", It.IsAny<CancellationToken>()), Times.Once);
        protection.Verify(
            service => service.CheckTokenRequest(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateToken_WhenRequestedTooFrequently_Returns429AfterValidatingCredentials()
    {
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateTokenAsync("EAP-001", "secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse());
        var localizer = new Mock<IMessageLocalizer>();
        localizer.Setup(localization => localization["Success"]).Returns("Success");
        localizer.Setup(localization => localization["TokenRequestTooFrequent"]).Returns("Too frequent");
        var protection = new Mock<IEapClientRequestProtection>();
        protection.SetupSequence(service => service.CheckTokenRequest("EAP-001"))
            .Returns(RequestLimitDecision.Allow())
            .Returns(RequestLimitDecision.Allow())
            .Returns(RequestLimitDecision.Allow())
            .Returns(RequestLimitDecision.Reject(RequestLimitReason.TokenBlocked, 300, true));
        var controller = CreateController(tokenService.Object, localizer.Object, protection.Object);
        var request = new TokenRequest { ClientId = "EAP-001", ClientSecret = "secret" };

        await controller.CreateToken(request, CancellationToken.None);
        await controller.CreateToken(request, CancellationToken.None);
        await controller.CreateToken(request, CancellationToken.None);
        var result = await controller.CreateToken(request, CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, controller.Response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal("Too frequent", result.Message);
        tokenService.Verify(
            service => service.CreateTokenAsync("EAP-001", "secret", It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task CreateToken_WhenCredentialsAreInvalid_DoesNotConsumeClientTokenLimit()
    {
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateTokenAsync("EAP-001", "wrong-secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);
        tokenService.Setup(service => service.CreateTokenAsync("EAP-001", "correct-secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse());
        var localizer = new Mock<IMessageLocalizer>();
        localizer.Setup(localization => localization["Success"]).Returns("Success");
        var protection = new Mock<IEapClientRequestProtection>();
        protection.Setup(service => service.CheckTokenRequest("EAP-001"))
            .Returns(RequestLimitDecision.Allow());
        var controller = CreateController(tokenService.Object, localizer.Object, protection.Object);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var invalidRequest = new TokenRequest { ClientId = "EAP-001", ClientSecret = "wrong-secret" };
            await Assert.ThrowsAsync<EAP.Application.Exceptions.AuthenticationFailedException>(
                () => controller.CreateToken(invalidRequest, CancellationToken.None));
        }

        var validRequest = new TokenRequest { ClientId = "EAP-001", ClientSecret = "correct-secret" };
        var result = await controller.CreateToken(validRequest, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(StatusCodes.Status200OK, controller.Response.StatusCode);
        protection.Verify(
            service => service.CheckTokenRequest("EAP-001"),
            Times.Once);
    }

    private static AuthController CreateController(ITokenService tokenService, IMessageLocalizer localizer, IEapClientRequestProtection protection)
    {
        var controller = new AuthController(tokenService, localizer, protection, NullLogger<AuthController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }
}
