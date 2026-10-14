using EAP.Models.Entities.Quality;

namespace EAP.Application.Interfaces;

public interface ISpcInspectionRepository
{
    Task<string?> GetProcessCodeAsync(string project, string process, CancellationToken cancellationToken);

    Task<bool> IsEquipmentConfiguredAsync(string processCode, string equipmentId, CancellationToken cancellationToken);

    Task<SpcInspectionEntity?> GetInspectionAsync(string equipmentId, string processCode, CancellationToken cancellationToken);
}
