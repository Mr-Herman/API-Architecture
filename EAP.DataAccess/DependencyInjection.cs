using Dapper;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.DataAccess.Database;
using EAP.DataAccess.Database.SqlServer;
using EAP.DataAccess.Repositories;
using EAP.DataAccess.Repositories.Commands;
using EAP.DataAccess.Repositories.Quality;
using EAP.DataAccess.Security;
using Microsoft.Extensions.DependencyInjection;

namespace EAP.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDataServices(this IServiceCollection services)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<IConnectionStringProtector, AesConnectionStringProtector>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<ISqlServerBulkWriter, SqlServerBulkWriter>();

        services.AddScoped<IProcessAssemblyRepository, ProcessAssemblyRepository>();
        services.AddScoped<IEquipmentQueryRepository, EquipmentQueryRepository>();
        services.AddScoped<ICarrierQueryRepository, CarrierQueryRepository>();
        services.AddScoped<IMaterialQueryRepository, MaterialQueryRepository>();
        services.AddScoped<IToolQueryRepository, ToolQueryRepository>();
        services.AddScoped<IOperatorQueryRepository, OperatorQueryRepository>();
        services.AddScoped<IProductionQueryRepository, ProductionQueryRepository>();
        services.AddScoped<IProductionTrackingRepository, ProductionTrackingRepository>();
        services.AddScoped<IQaInspectionRepository, QaInspectionRepository>();
        services.AddScoped<ISpcInspectionRepository, SpcInspectionRepository>();
        services.AddScoped<IEquipmentCommandRepository, EquipmentCommandRepository>();
        services.AddScoped<IToolCommandRepository, ToolCommandRepository>();
        services.AddScoped<IMaterialCommandRepository, MaterialCommandRepository>();
        services.AddScoped<ICarrierCommandRepository, CarrierCommandRepository>();
        services.AddScoped<IHoldLotRepository, HoldLotRepository>();
        services.AddScoped<INotificationCommandRepository, NotificationCommandRepository>();

        return services;
    }
}
