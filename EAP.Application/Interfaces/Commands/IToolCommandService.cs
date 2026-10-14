using EAP.Models.Common;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IToolCommandService
{
    Task<ApiResult<OperationResponse>> ConfirmSetupAsync(ToolSetupRequest request, CancellationToken cancellationToken);

    Task<ApiResult<ToolVerificationResponse>> VerifyAsync(VerifyToolRequest request, CancellationToken cancellationToken);
}
