using EAP.Api.Controllers;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;
using Moq;

namespace EAP.UnitTests.Api.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task CreateToken_ForwardsCredentialsAndReturnsServiceResult()
    {
        var tokenService = new Mock<ITokenService>();
        var localizer = new Mock<IMessageLocalizer>();
        var request = new TokenRequest { ClientId = "client", ClientSecret = "secret" };
        var expected = ApiResult<TokenResponse>.SuccessResult(null);
        tokenService.Setup(x => x.CreateTokenAsync("client", "secret", It.IsAny<CancellationToken>())).ReturnsAsync((TokenResponse?)null);
        var controller = new AuthController(tokenService.Object, localizer.Object);

        await Assert.ThrowsAsync<EAP.Application.Exceptions.AuthenticationFailedException>(() => controller.CreateToken(request, CancellationToken.None));
        tokenService.Verify(x => x.CreateTokenAsync("client", "secret", It.IsAny<CancellationToken>()), Times.Once);
    }
}
