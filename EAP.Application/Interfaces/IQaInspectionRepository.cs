using EAP.Models.Entities.Quality;

namespace EAP.Application.Interfaces;

public interface IQaInspectionRepository
{
    Task<string?> GetProcessCodeAsync(string project, string process, CancellationToken cancellationToken);

    Task<bool> IsEquipmentConfiguredAsync(string processCode, string equipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FaiInspectionEntity>> GetLatestInspectionsAsync(string equipmentId, string processCode, CancellationToken cancellationToken);
}
