namespace EAP.Application.Interfaces;

using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;

public interface ICarrierValidationService
{
    Task<ApiResult<ValidateLotCarrierResponse>> ValidateLotAsync(ValidateLotCarrierRequest request, CancellationToken cancellationToken);
}
