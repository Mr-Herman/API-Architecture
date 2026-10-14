using EAP.Models.Entities.Equipment;

namespace EAP.Application.Interfaces;

public interface IEquipmentQueryRepository
{
    Task<EquipmentStatusEntity?> GetStatusAsync(string equipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipmentProcessEntity>> GetProcessesAsync(string equipmentId, CancellationToken cancellationToken);
}
