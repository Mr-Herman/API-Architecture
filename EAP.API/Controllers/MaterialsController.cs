using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Materials;
using EAP.Models.Responses.Materials;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Material")]
public sealed class MaterialsController : ControllerBase
{
    private readonly IMaterialQueryService _service;
    private readonly IMaterialCommandService _commandService;

    public MaterialsController(IMaterialQueryService service, IMaterialCommandService commandService)
    {
        _service = service;
        _commandService = commandService;
    }

    [HttpPost("GetEqpMaterialSetup")]
    public Task<ApiResult<IReadOnlyList<EquipmentMaterialResponse>>> GetEquipmentSetup([FromBody] EquipmentMaterialRequest request, CancellationToken cancellationToken)
    {
        return _service.GetEquipmentSetupAsync(request, cancellationToken);
    }

    [HttpPost("GetMaterialInformation")]
    public Task<ApiResult<IReadOnlyList<MaterialInformationResponse>>> GetInformation([FromBody] MaterialInformationRequest request, CancellationToken cancellationToken)
    {
        return _service.GetInformationAsync(request, cancellationToken);
    }

    [HttpPost("ConfirmMaterialSetup")]
    public Task<ApiResult<OperationResponse>> ConfirmSetup([FromBody] MaterialSetupRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ConfirmSetupAsync(request, cancellationToken);
    }
}
