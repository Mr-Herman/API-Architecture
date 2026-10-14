using EAP.Models.Common;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;

namespace EAP.Application.Interfaces;

public interface IToolQueryService
{
    Task<ApiResult<ToolLotInformationResponse>> GetLotInformationAsync(ToolLotRequest request, CancellationToken cancellationToken);

    Task<ApiResult<IReadOnlyList<EquipmentToolResponse>>> GetEquipmentSetupAsync(EquipmentToolRequest request, CancellationToken cancellationToken);
}
