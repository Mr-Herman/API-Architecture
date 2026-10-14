using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Materials;
using EAP.Models.Responses.Materials;

namespace EAP.Application.Services;

public sealed class MaterialQueryService : IMaterialQueryService
{
    private readonly IMaterialQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public MaterialQueryService(IMaterialQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<IReadOnlyList<EquipmentMaterialResponse>>> GetEquipmentSetupAsync(EquipmentMaterialRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetEquipmentSetupAsync));
        var result = await _repository.GetEquipmentSetupAsync(request.EquipmentId.Trim(), cancellationToken);
        var responses = result.Select(MaterialResponseMapper.ToResponse).ToArray();
        return ApiResult<IReadOnlyList<EquipmentMaterialResponse>>.SuccessResult(responses, _localizer["Success"]);
    }

    public async Task<ApiResult<IReadOnlyList<MaterialInformationResponse>>> GetInformationAsync(MaterialInformationRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetInformationAsync));
        var result = await _repository.GetInformationAsync(request.MaterialLot.Trim(), cancellationToken);
        var responses = result.Select(MaterialResponseMapper.ToResponse).ToArray();
        return ApiResult<IReadOnlyList<MaterialInformationResponse>>.SuccessResult(responses, _localizer["Success"]);
    }
}
