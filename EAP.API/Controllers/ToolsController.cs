using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("api/Tool")]
public sealed class ToolsController : ControllerBase
{
    private readonly IToolQueryService _service;
    private readonly IToolCommandService _commandService;

    public ToolsController(IToolQueryService service, IToolCommandService commandService)
    {
        _service = service;
        _commandService = commandService;
    }

    [HttpPost("GetToolLotInformation")]
    public Task<ApiResult<ToolLotInformationResponse>> GetLotInformation([FromBody] ToolLotRequest request, CancellationToken cancellationToken)
    {
        return _service.GetLotInformationAsync(request, cancellationToken);
    }

    [HttpPost("GetEqpToolSetup")]
    public Task<ApiResult<IReadOnlyList<EquipmentToolResponse>>> GetEquipmentSetup([FromBody] EquipmentToolRequest request, CancellationToken cancellationToken)
    {
        return _service.GetEquipmentSetupAsync(request, cancellationToken);
    }

    [HttpPost("ConfirmToolSetup")]
    public Task<ApiResult<OperationResponse>> ConfirmSetup([FromBody] ToolSetupRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ConfirmSetupAsync(request, cancellationToken);
    }
}
