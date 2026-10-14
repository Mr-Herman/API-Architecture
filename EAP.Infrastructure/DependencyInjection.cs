using EAP.Application.Interfaces;
using EAP.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace EAP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ILegacyPasswordEncoder, LegacyPasswordEncoder>();

        return services;
    }
}
