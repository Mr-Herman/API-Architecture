using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;

namespace EAP.Application.Interfaces;

public interface IEquipmentQueryService
{
    Task<ApiResult<EquipmentStatusResponse>> GetStatusAsync(EquipmentQueryRequest request, CancellationToken cancellationToken);

    Task<ApiResult<IReadOnlyList<EquipmentProcessResponse>>> GetProcessesAsync(EquipmentQueryRequest request, CancellationToken cancellationToken);
}
