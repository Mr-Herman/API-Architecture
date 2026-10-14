using System.ComponentModel.DataAnnotations;
using EAP.Application.Interfaces;
using EAP.Models.Common;
using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Authorize]
[Route("test/ProcessAssembly")]
public sealed class ProcessAssembliesController : ControllerBase
{
    private readonly IProcessAssemblyService _service;

    public ProcessAssembliesController(IProcessAssemblyService service)
    {
        _service = service;
    }

    [HttpGet("Search")]
    public async Task<ApiResult<PagedResult<ProcessAssemblyResponse>>> Search([FromQuery] ProcessAssemblyQueryRequest request, CancellationToken cancellationToken)
    {
        return await _service.SearchAsync(request, cancellationToken);
    }

    [HttpGet("GetById")]
    public async Task<ApiResult<ProcessAssemblyResponse>> GetById([FromRoute, Range(1, long.MaxValue)] long id, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(id, cancellationToken);
    }

    [HttpPost("Create")]
    public async Task<ApiResult<ProcessAssemblyResponse>> Create([FromBody] CreateProcessAssemblyRequest request, CancellationToken cancellationToken)
    {
        return await _service.CreateAsync(request, cancellationToken);
    }

    [HttpPut("Update")]
    public async Task<ApiResult<ProcessAssemblyResponse>> Update([FromRoute, Range(1, long.MaxValue)] long id, [FromBody] UpdateProcessAssemblyRequest request, CancellationToken cancellationToken)
    {
        return await _service.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("Delete")]
    public async Task<ApiResult<object>> Delete([FromRoute, Range(1, long.MaxValue)] long id, CancellationToken cancellationToken)
    {
        return await _service.DeleteAsync(id, cancellationToken);
    }
}
