using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/HoldLot")]
public sealed class HoldLotsController : ControllerBase
{
    private readonly IHoldLotService _service;

    public HoldLotsController(IHoldLotService service)
    {
        _service = service;
    }

    [HttpPost("HoldLot")]
    public Task<ApiResult<OperationResponse>> Hold([FromBody] HoldLotRequest request, CancellationToken cancellationToken)
    {
        return _service.HoldAsync(request, cancellationToken);
    }
}
