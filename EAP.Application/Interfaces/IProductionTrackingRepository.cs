using EAP.Models.Entities.Tracking;
using EAP.Models.Requests.Production;

namespace EAP.Application.Interfaces;

public interface IProductionTrackingRepository
{
    Task<bool> IsTransactionCompletedAsync(string transactionId, CancellationToken cancellationToken);
    Task<ProductionTrackingContextEntity?> GetTrackInContextAsync(TrackInRequest request, CancellationToken cancellationToken);
    Task<ProductionTrackingContextEntity?> GetTrackOutContextAsync(TrackOutRequest request, CancellationToken cancellationToken);
    Task<ProductionLotWipEntity?> GetLotWipAsync(string lotId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionSnWipEntity>> GetSnWipsAsync(string lotId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetDefectCodesAsync(string workFlow, string process, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionMaterialEntity>> GetMaterialsAsync(string productPartNumber, string workFlow, string process, string equipmentId, CancellationToken cancellationToken);
    Task<int> ExecuteTrackInAsync(TrackInRequest request, ProductionTrackingContextEntity context, CancellationToken cancellationToken);
    Task<int> ExecuteTrackOutAsync(TrackOutRequest request, ProductionTrackingContextEntity context, ProductionLotWipEntity lotWip, IReadOnlyList<ProductionSnWipEntity> snWips, IReadOnlyList<ProductionMaterialEntity> materials, CancellationToken cancellationToken);
}
