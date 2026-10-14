using EAP.Models.Entities.Tools;

namespace EAP.Application.Interfaces;

public interface IToolQueryRepository
{
    Task<ToolLotInformationEntity?> GetLotInformationAsync(string toolId, string equipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipmentToolEntity>> GetEquipmentSetupAsync(string equipmentId, CancellationToken cancellationToken);
}
