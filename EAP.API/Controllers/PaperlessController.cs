using EAP.Application.Interfaces;
using EAP.Models.Common;
using EAP.Models.Requests.Quality;
using EAP.Models.Responses.Quality;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("paperless")]
public sealed class PaperlessController : ControllerBase
{
    private readonly IQualityInspectionService _service;

    public PaperlessController(IQualityInspectionService service)
    {
        _service = service;
    }

    [HttpPost("GetSPCFAIResult")]
    public Task<ApiResult<QualityInspectionResponse>> GetSpcFaiResult([FromBody] QualityInspectionRequest request, CancellationToken cancellationToken)
    {
        return _service.ValidateInspectionAsync(request, cancellationToken);
    }
}
