using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EAP.Application.Interfaces;
using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;
using EAP.Models.Constants;
using EAP.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EAP.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, ILogger<JwtTokenService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public Task<TokenResponse?> CreateTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = ResolveClient(clientId, clientSecret);

        if (client is null)
        {
            _logger.LogWarning("[AUTH_FAILED] Token request rejected. ClientId={ClientId}", clientId);
            return Task.FromResult<TokenResponse?>(null);
        }

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtClaimNames.ClientName, client.ClientName),
            new(JwtClaimNames.ClientSystem, client.ClientId),
            new(JwtClaimNames.EapClientId, clientId)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        _logger.LogDebug(
            "[AUTH_SUCCESS] Access token created. ClientSystem={ClientSystem} EapClientId={EapClientId} ExpiresAtUtc={ExpiresAtUtc}",
            client.ClientId,
            clientId,
            expires);
        return Task.FromResult<TokenResponse?>(new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = (int)TimeSpan.FromMinutes(_jwtOptions.AccessTokenMinutes).TotalSeconds,
            ExpiresAtUtc = expires
        });
    }

    private JwtClientOptions? ResolveClient(string clientId, string clientSecret)
    {
        var exactClient = _jwtOptions.Clients.FirstOrDefault(client =>
            string.Equals(client.ClientId, clientId, StringComparison.Ordinal));

        if (exactClient is not null)
        {
            return FixedTimeEquals(exactClient.ClientSecret, clientSecret)
                ? exactClient
                : null;
        }

        return _jwtOptions.Clients.FirstOrDefault(client =>
            client.AcceptEapClientIds
            && FixedTimeEquals(client.ClientSecret, clientSecret));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
