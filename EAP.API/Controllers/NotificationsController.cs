using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Notification")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationCommandService _service;

    public NotificationsController(INotificationCommandService service)
    {
        _service = service;
    }

    [HttpPost("SendDashboardMessage")]
    public Task<ApiResult<OperationResponse>> SendDashboardMessage([FromBody] DashboardMessageRequest request, CancellationToken cancellationToken)
    {
        return _service.SendDashboardMessageAsync(request, cancellationToken);
    }
}
