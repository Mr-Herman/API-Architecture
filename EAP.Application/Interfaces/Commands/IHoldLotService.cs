using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IHoldLotService
{
    Task<ApiResult<OperationResponse>> HoldAsync(HoldLotRequest request, CancellationToken cancellationToken);
}
