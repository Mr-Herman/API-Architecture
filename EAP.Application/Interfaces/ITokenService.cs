using EAP.Models.Requests.Auth;
using EAP.Models.Responses.Auth;

namespace EAP.Application.Interfaces;

public interface ITokenService
{
    Task<TokenResponse?> CreateTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}
