using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;

namespace EAP.Application.Interfaces.Commands;

public interface IToolCommandRepository
{
    Task<int> InsertSetupAsync(ToolSetupRequest request, CancellationToken cancellationToken);

    Task<ToolVerificationResponse> VerifyAsync(VerifyToolRequest request, CancellationToken cancellationToken);
}
