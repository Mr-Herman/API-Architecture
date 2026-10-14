using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface ICarrierCommandService
{
    Task<ApiResult<OperationResponse>> ConfirmLotSetupAsync(CarrierLotSetupRequest request, CancellationToken cancellationToken);

    Task<ApiResult<OperationResponse>> RemoveLotSetupAsync(RemoveCarrierLotSetupRequest request, CancellationToken cancellationToken);
}
