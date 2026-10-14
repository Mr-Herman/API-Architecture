using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Production;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Production")]
public sealed class ProductionController : ControllerBase
{
    private readonly IProductionQueryService _service;
    private readonly IToolCommandService _toolCommandService;
    private readonly IProductionTrackingService _trackingService;

    public ProductionController(IProductionQueryService service, IToolCommandService toolCommandService, IProductionTrackingService trackingService)
    {
        _service = service;
        _toolCommandService = toolCommandService;
        _trackingService = trackingService;
    }

    [HttpPost("GetProcessDefectList")]
    public Task<ApiResult<IReadOnlyList<ProcessDefectResponse>>> GetProcessDefects([FromBody] ProcessDefectRequest request, CancellationToken cancellationToken)
    {
        return _service.GetProcessDefectsAsync(request, cancellationToken);
    }

    [HttpPost("GetLotInformation")]
    public Task<ApiResult<LotInformationResponse>> GetLotInformation([FromBody] LotInformationRequest request, CancellationToken cancellationToken)
    {
        return _service.GetLotInformationAsync(request, cancellationToken);
    }

    [HttpPost("VerifyTool")]
    public Task<ApiResult<ToolVerificationResponse>> VerifyTool([FromBody] VerifyToolRequest request, CancellationToken cancellationToken)
    {
        return _toolCommandService.VerifyAsync(request, cancellationToken);
    }

    [HttpPost("TrackIn")]
    public Task<ApiResult<OperationResponse>> TrackIn([FromBody] TrackInRequest request, CancellationToken cancellationToken)
    {
        return _trackingService.TrackInAsync(request, cancellationToken);
    }

    [HttpPost("TrackOut")]
    public Task<ApiResult<OperationResponse>> TrackOut([FromBody] TrackOutRequest request, CancellationToken cancellationToken)
    {
        return _trackingService.TrackOutAsync(request, cancellationToken);
    }
}
