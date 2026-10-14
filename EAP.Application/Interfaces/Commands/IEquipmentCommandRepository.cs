using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;

namespace EAP.Application.Interfaces.Commands;

public interface IEquipmentCommandRepository
{
    Task<int> UpsertOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken);

    Task<int> InsertOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken);

    Task<int> RemoveOperatorAsync(RemoveOperatorBindingRequest request, CancellationToken cancellationToken);

    Task<int> ChangeStatusAsync(EquipmentStatusChangeRequest request, CancellationToken cancellationToken);
}
