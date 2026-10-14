using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces.Commands;

public interface INotificationCommandRepository
{
    Task<int> SendDashboardMessageAsync(DashboardMessageRequest request, CancellationToken cancellationToken);
}
