using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IHoldLotRepository
{
    Task<(string? WorkFlow, string? Process, string? OperatorId)?> GetEquipmentProcessAsync(string equipmentId, CancellationToken cancellationToken);

    Task<decimal> GetNextHoldNumberAsync(CancellationToken cancellationToken);

    Task<int> HoldAsync(HoldLotRequest request, CancellationToken cancellationToken);
}
