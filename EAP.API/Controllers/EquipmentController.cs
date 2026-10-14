using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Equipment")]
public sealed class EquipmentController : ControllerBase
{
    private readonly IEquipmentQueryService _service;
    private readonly IEquipmentCommandService _commandService;

    public EquipmentController(IEquipmentQueryService service, IEquipmentCommandService commandService)
    {
        _service = service;
        _commandService = commandService;
    }

    [HttpPost("GetEqpStatus")]
    public Task<ApiResult<EquipmentStatusResponse>> GetStatus([FromBody] EquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        return _service.GetStatusAsync(request, cancellationToken);
    }

    [HttpPost("GetEqpProcess")]
    public Task<ApiResult<IReadOnlyList<EquipmentProcessResponse>>> GetProcesses([FromBody] EquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        return _service.GetProcessesAsync(request, cancellationToken);
    }

    [HttpPost("SetEqpStatus")]
    public Task<ApiResult<OperationResponse>> ChangeStatus([FromBody] EquipmentStatusChangeRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ChangeStatusAsync(request, cancellationToken);
    }
}
