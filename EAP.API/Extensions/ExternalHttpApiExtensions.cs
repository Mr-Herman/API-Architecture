using EAP.Application.Interfaces;
using EAP.Infrastructure.Http;

namespace EAP.Api.Extensions;

public static class ExternalHttpApiExtensions
{
    public static IServiceCollection AddExternalHttpApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IHttpApiClient, HttpApiClient>();
        return services;
    }
}
