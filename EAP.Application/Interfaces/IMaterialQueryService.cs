using EAP.Models.Common;
using EAP.Models.Requests.Materials;
using EAP.Models.Responses.Materials;

namespace EAP.Application.Interfaces;

public interface IMaterialQueryService
{
    Task<ApiResult<IReadOnlyList<EquipmentMaterialResponse>>> GetEquipmentSetupAsync(EquipmentMaterialRequest request, CancellationToken cancellationToken);

    Task<ApiResult<IReadOnlyList<MaterialInformationResponse>>> GetInformationAsync(MaterialInformationRequest request, CancellationToken cancellationToken);
}
