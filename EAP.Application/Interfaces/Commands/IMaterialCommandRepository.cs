using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface IMaterialCommandRepository
{
    Task<int> ReplaceSetupAsync(MaterialSetupRequest request, CancellationToken cancellationToken);
}
