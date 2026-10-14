using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IMaterialCommandService
{
    Task<ApiResult<OperationResponse>> ConfirmSetupAsync(MaterialSetupRequest request, CancellationToken cancellationToken);
}
