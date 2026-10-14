using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Services;
using EAP.Application.Services.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace EAP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProcessAssemblyService, ProcessAssemblyService>();
        services.AddScoped<IEquipmentQueryService, EquipmentQueryService>();
        services.AddScoped<ICarrierQueryService, CarrierQueryService>();
        services.AddScoped<IMaterialQueryService, MaterialQueryService>();
        services.AddScoped<IToolQueryService, ToolQueryService>();
        services.AddScoped<IOperatorQueryService, OperatorQueryService>();
        services.AddScoped<IOperatorAuthenticationService, OperatorAuthenticationService>();
        services.AddScoped<ICarrierValidationService, CarrierValidationService>();
        services.AddScoped<IProductionQueryService, ProductionQueryService>();
        services.AddScoped<IProductionTrackingService, ProductionTrackingService>();
        services.AddScoped<IQualityInspectionService, QualityInspectionService>();
        services.AddScoped<IEquipmentCommandService, EquipmentCommandService>();
        services.AddScoped<IToolCommandService, ToolCommandService>();
        services.AddScoped<IMaterialCommandService, MaterialCommandService>();
        services.AddScoped<ICarrierCommandService, CarrierCommandService>();
        services.AddScoped<IHoldLotService, HoldLotService>();
        services.AddScoped<INotificationCommandService, NotificationCommandService>();
        return services;
    }
}
