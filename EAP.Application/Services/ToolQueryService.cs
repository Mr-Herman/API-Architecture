using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;

namespace EAP.Application.Services;

public sealed class ToolQueryService : IToolQueryService
{
    private readonly IToolQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public ToolQueryService(IToolQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<ToolLotInformationResponse>> GetLotInformationAsync(ToolLotRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetLotInformationAsync));
        var result = await _repository.GetLotInformationAsync(request.ToolId.Trim(), request.EquipmentId.Trim(), cancellationToken);
        if (result is null)
        {
            throw new ResourceNotFoundException(_localizer["ToolLotInformationNotFound"]);
        }

        var responses = ToolResponseMapper.ToResponse(result);
        return ApiResult<ToolLotInformationResponse>.SuccessResult(responses, _localizer["Success"]);
    }

    public async Task<ApiResult<IReadOnlyList<EquipmentToolResponse>>> GetEquipmentSetupAsync(EquipmentToolRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetEquipmentSetupAsync));
        var result = await _repository.GetEquipmentSetupAsync(request.EquipmentId.Trim(), cancellationToken);
        var responses = result.Select(ToolResponseMapper.ToResponse).ToArray();
        return ApiResult<IReadOnlyList<EquipmentToolResponse>>.SuccessResult(responses, _localizer["Success"]);
    }
}
