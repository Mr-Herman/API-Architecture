using EAP.Models.Common;
using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;

namespace EAP.Application.Interfaces;

public interface IProcessAssemblyService
{
    Task<ApiResult<PagedResult<ProcessAssemblyResponse>>> SearchAsync(ProcessAssemblyQueryRequest request, CancellationToken cancellationToken);

    Task<ApiResult<ProcessAssemblyResponse>> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<ApiResult<ProcessAssemblyResponse>> CreateAsync(CreateProcessAssemblyRequest request, CancellationToken cancellationToken);

    Task<ApiResult<ProcessAssemblyResponse>> UpdateAsync(long id, UpdateProcessAssemblyRequest request, CancellationToken cancellationToken);

    Task<ApiResult<object>> DeleteAsync(long id, CancellationToken cancellationToken);
}
