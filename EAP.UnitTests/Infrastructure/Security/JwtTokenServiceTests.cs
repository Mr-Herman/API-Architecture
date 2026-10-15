using System.IdentityModel.Tokens.Jwt;
using EAP.Infrastructure.Options;
using EAP.Infrastructure.Security;
using EAP.Models.Constants;
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
        Assert.Equal("UnitTestClient", token.Claims.Single(x => x.Type == JwtClaimNames.ClientName).Value);
        Assert.Equal("client-1", token.Claims.Single(x => x.Type == JwtClaimNames.ClientSystem).Value);
        Assert.Equal("client-1", token.Claims.Single(x => x.Type == JwtClaimNames.EapClientId).Value);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenEquipmentClientIdsAreEnabled_UsesRequestedEapClientId()
    {
        var service = CreateService(true);

        var result = await service.CreateTokenAsync("EQ-1001", "secret-1", CancellationToken.None);

        Assert.NotNull(result);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("EQ-1001", token.Claims.Single(x => x.Type == JwtClaimNames.EapClientId).Value);
        Assert.Equal("client-1", token.Claims.Single(x => x.Type == JwtClaimNames.ClientSystem).Value);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenSecretIsInvalid_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.CreateTokenAsync("client-1", "wrong-secret", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenRegisteredClientUsesWrongSecret_DoesNotUseDynamicClientFallback()
    {
        var jwtOptions = CreateOptions();
        jwtOptions.Clients.Add(new JwtClientOptions
        {
            ClientId = "client-1",
            ClientSecret = "secret-1",
            ClientName = "UnitTestClient"
        });
        jwtOptions.Clients.Add(new JwtClientOptions
        {
            ClientId = "equipment-system",
            ClientSecret = "equipment-secret",
            ClientName = "EquipmentSystem",
            AcceptEapClientIds = true
        });
        var service = CreateService(jwtOptions);

        var result = await service.CreateTokenAsync(
            "client-1",
            "equipment-secret",
            CancellationToken.None);

        Assert.Null(result);
    }

    private static JwtTokenService CreateService(bool acceptEapClientIds = false)
    {
        var client = new JwtClientOptions
        {
            ClientId = "client-1",
            ClientSecret = "secret-1",
            ClientName = "UnitTestClient",
            AcceptEapClientIds = acceptEapClientIds
        };

        var clients = new List<JwtClientOptions>
        {
            client
        };

        var jwtOptions = CreateOptions();
        jwtOptions.Clients = clients;

        return CreateService(jwtOptions);
    }

    private static JwtOptions CreateOptions()
    {
        return new JwtOptions
        {
            SigningKey = "unit-test-signing-key-with-at-least-32-characters",
            Issuer = "UnitTests",
            Audience = "EAP",
            AccessTokenMinutes = 30
        };
    }

    private static JwtTokenService CreateService(JwtOptions jwtOptions)
    {
        var options = Options.Create(jwtOptions);
        var logger = NullLogger<JwtTokenService>.Instance;

        return new JwtTokenService(options, logger);
    }

}
