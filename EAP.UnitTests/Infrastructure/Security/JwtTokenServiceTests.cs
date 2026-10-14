using System.IdentityModel.Tokens.Jwt;
using EAP.Infrastructure.Options;
using EAP.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EAP.UnitTests.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateTokenAsync_WhenCredentialsAreValid_ReturnsToken()
    {
        var service = CreateService();
        var result = await service.CreateTokenAsync("client-1", "secret-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(1800, result.ExpiresIn);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("UnitTestClient", token.Claims.Single(x => x.Type == "ClientName").Value);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenSecretIsInvalid_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.CreateTokenAsync("client-1", "wrong-secret", CancellationToken.None);

        Assert.Null(result);
    }

    private static JwtTokenService CreateService()
    {
        var client = new JwtClientOptions
        {
            ClientId = "client-1",
            ClientSecret = "secret-1",
            ClientName = "UnitTestClient"
        };

        var clients = new List<JwtClientOptions>
        {
            client
        };

        var jwtOptions = new JwtOptions
        {
            SigningKey = "unit-test-signing-key-with-at-least-32-characters",
            Issuer = "UnitTests",
            Audience = "EAP",
            AccessTokenMinutes = 30,
            Clients = clients
        };

        var options = Options.Create(jwtOptions);
        var logger = NullLogger<JwtTokenService>.Instance;

        return new JwtTokenService(options, logger);
    }

}
