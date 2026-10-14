using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;

namespace EAP.Application.Interfaces;

public interface ICarrierQueryService
{
    Task<ApiResult<CarrierSetupResponse>> GetSetupAsync(GetCarrierSetupRequest request, CancellationToken cancellationToken);
}
