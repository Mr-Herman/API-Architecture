using EAP.Application.Interfaces.Commands;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services.Commands;

public sealed class HoldLotService : IHoldLotService
{
    private readonly IHoldLotRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public HoldLotService(IHoldLotRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> HoldAsync(HoldLotRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(HoldAsync));

        var process = await _repository.GetEquipmentProcessAsync(request.EquipmentId, cancellationToken);
        if (process is null)
        {
            throw new InvalidOperationException(_localizer["NoOperatorProcessBinding"]);
        }

        request.WorkFlow = process.Value.WorkFlow;
        request.Process = process.Value.Process;
        request.OperatorId = process.Value.OperatorId ?? request.OperatorId;
        request.HoldNumber = await _repository.GetNextHoldNumberAsync(cancellationToken);

        var affected = await _repository.HoldAsync(request, cancellationToken);
        var response = OperationResponseMapper.ToResponse(affected);

        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
