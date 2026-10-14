using EAP.Models.Entities.Production;

namespace EAP.Application.Interfaces;

public interface IProductionQueryRepository
{
    Task<ProductionEquipmentOperationEntity?> GetEquipmentOperationAsync(string equipmentId, string? workFlow, string? process, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductionLotEntity>> GetLotRowsAsync(string workFlow, string lotIdOrSn, CancellationToken cancellationToken);

    Task<ProductionLotEntity?> GetLotMasterAsync(string lotId, CancellationToken cancellationToken);

    Task<ProductionRecipeEntity?> GetRecipeAsync(string projectCode, string workFlow, string process, string formNo, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetToolRequirementsAsync(string productPartNumber, string workFlow, string process, string equipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductionMaterialRequirementEntity>> GetMaterialRequirementsAsync(string productPartNumber, string workFlow, string process, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProcessDefectEntity>> GetProcessDefectsAsync(string workFlow, string process, CancellationToken cancellationToken);
}
