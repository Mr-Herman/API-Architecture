using EAP.Models.Entities.ProcessAssemblies;
using EAP.Models.Requests.ProcessAssemblies;

namespace EAP.Application.Interfaces;

public interface IProcessAssemblyRepository
{
    Task<(IReadOnlyList<ProcessAssemblyEntity> Items, long TotalCount)> SearchAsync(ProcessAssemblyQueryRequest request, CancellationToken cancellationToken);

    Task<ProcessAssemblyEntity?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken);

    Task<long> CreateAsync(ProcessAssemblyEntity entity, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(ProcessAssemblyEntity entity, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
}
