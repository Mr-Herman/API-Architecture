using EAP.Models.Common;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces;

public interface IProductionTrackingService
{
    Task<ApiResult<OperationResponse>> TrackInAsync(TrackInRequest request, CancellationToken cancellationToken);
    Task<ApiResult<OperationResponse>> TrackOutAsync(TrackOutRequest request, CancellationToken cancellationToken);
}
