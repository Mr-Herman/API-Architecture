using EAP.Models.Entities.Materials;

namespace EAP.Application.Interfaces;

public interface IMaterialQueryRepository
{
    Task<IReadOnlyList<EquipmentMaterialEntity>> GetEquipmentSetupAsync(string equipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MaterialInformationEntity>> GetInformationAsync(string materialLot, CancellationToken cancellationToken);
}
