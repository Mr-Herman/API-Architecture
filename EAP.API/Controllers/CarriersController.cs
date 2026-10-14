using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Carrier")]
public sealed class CarriersController : ControllerBase
{
    private readonly ICarrierQueryService _service;
    private readonly ICarrierCommandService _commandService;
    private readonly ICarrierValidationService _validationService;

    public CarriersController(ICarrierQueryService service, ICarrierCommandService commandService, ICarrierValidationService validationService)
    {
        _service = service;
        _commandService = commandService;
        _validationService = validationService;
    }

    [HttpPost("GetCarrierSetUpInformation")]
    public Task<ApiResult<CarrierSetupResponse>> GetSetup([FromBody] GetCarrierSetupRequest request, CancellationToken cancellationToken)
    {
        return _service.GetSetupAsync(request, cancellationToken);
    }

    [HttpPost("ValidateLotCarrier")]
    public Task<ApiResult<ValidateLotCarrierResponse>> ValidateLot([FromBody] ValidateLotCarrierRequest request, CancellationToken cancellationToken)
    {
        return _validationService.ValidateLotAsync(request, cancellationToken);
    }

    [HttpPost("ConfirmCarrierLotSetup")]
    public Task<ApiResult<OperationResponse>> ConfirmLotSetup([FromBody] CarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ConfirmLotSetupAsync(request, cancellationToken);
    }

    [HttpPost("RemoveCarrierLotSetup")]
    public Task<ApiResult<OperationResponse>> RemoveLotSetup([FromBody] RemoveCarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        return _commandService.RemoveLotSetupAsync(request, cancellationToken);
    }
}
