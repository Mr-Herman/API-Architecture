using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IEquipmentCommandService
{
    Task<ApiResult<OperationResponse>> ConfirmOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken);

    Task<ApiResult<OperationResponse>> RemoveOperatorAsync(RemoveOperatorBindingRequest request, CancellationToken cancellationToken);

    Task<ApiResult<OperationResponse>> ChangeStatusAsync(EquipmentStatusChangeRequest request, CancellationToken cancellationToken);
}
