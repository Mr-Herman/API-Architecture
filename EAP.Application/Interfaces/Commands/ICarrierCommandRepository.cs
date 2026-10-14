using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface ICarrierCommandRepository
{
    Task<int> ConfirmLotSetupAsync(CarrierLotSetupRequest request, CancellationToken cancellationToken);

    Task<int> RemoveLotSetupAsync(RemoveCarrierLotSetupRequest request, CancellationToken cancellationToken);
}
