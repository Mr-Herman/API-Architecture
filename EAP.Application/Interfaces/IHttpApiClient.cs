using EAP.Application.Http;

namespace EAP.Application.Interfaces;

public interface IHttpApiClient
{
    Task<HttpApiResponse> SendAsync(HttpApiRequest request, CancellationToken cancellationToken = default);
}
