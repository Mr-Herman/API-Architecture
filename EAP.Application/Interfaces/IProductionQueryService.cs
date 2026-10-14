using EAP.Models.Common;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Production;

namespace EAP.Application.Interfaces;

public interface IProductionQueryService
{
    Task<ApiResult<IReadOnlyList<ProcessDefectResponse>>> GetProcessDefectsAsync(ProcessDefectRequest request, CancellationToken cancellationToken);

    Task<ApiResult<LotInformationResponse>> GetLotInformationAsync(LotInformationRequest request, CancellationToken cancellationToken);
}
