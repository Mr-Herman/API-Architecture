using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using System.Threading;

namespace EAP.Application.Services;

public sealed class EquipmentQueryService : IEquipmentQueryService
{
    private readonly IEquipmentQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public EquipmentQueryService(IEquipmentQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<EquipmentStatusResponse>> GetStatusAsync(EquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetStatusAsync));
        var result = await _repository.GetStatusAsync(request.EquipmentId.Trim(), cancellationToken);
        if (result is null)
        {
            throw new ResourceNotFoundException(_localizer["EquipmentStatusNotFound"]);
        }

        var response = EquipmentResponseMapper.ToResponse(result);
        return ApiResult<EquipmentStatusResponse>.SuccessResult(response, _localizer["Success"]);
    }

    public async Task<ApiResult<IReadOnlyList<EquipmentProcessResponse>>> GetProcessesAsync(EquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetProcessesAsync));
        var result = await _repository.GetProcessesAsync(request.EquipmentId.Trim(), cancellationToken);
        var responses = result.Select(EquipmentResponseMapper.ToResponse).ToArray();
        return ApiResult<IReadOnlyList<EquipmentProcessResponse>>.SuccessResult(responses, _localizer["Success"]);
    }
}
