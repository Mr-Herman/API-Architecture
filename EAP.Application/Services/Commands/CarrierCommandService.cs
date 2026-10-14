using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services.Commands;

public sealed class CarrierCommandService : ICarrierCommandService
{
    private readonly ICarrierCommandRepository _repository;
    private readonly ICarrierQueryRepository _queryRepository;
    private readonly IMessageLocalizer _localizer;

    public CarrierCommandService(ICarrierCommandRepository repository, ICarrierQueryRepository queryRepository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _queryRepository = queryRepository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> ConfirmLotSetupAsync(CarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ConfirmLotSetupAsync));
        var canonicalLots = new List<string>();

        foreach (var inputLot in request.Lots)
        {
            var canonicalLot = await _queryRepository.ResolveLotIdAsync(inputLot.Trim(), cancellationToken);
            if (string.IsNullOrWhiteSpace(canonicalLot))
            {
                return ApiResult<OperationResponse>.FailResult(_localizer["LotNotFound"], 4004);
            }

            canonicalLots.Add(canonicalLot.Trim());
        }

        if (canonicalLots.Distinct(StringComparer.OrdinalIgnoreCase).Count() != canonicalLots.Count)
        {
            return ApiResult<OperationResponse>.FailResult(_localizer["DuplicateProductionLot"], 4005);
        }

        var normalizedRequest = new CarrierLotSetupRequest
        {
            TransactionId = request.TransactionId,
            CarrierId = request.CarrierId.Trim(),
            TransactionTime = request.TransactionTime,
            Lots = canonicalLots
        };
        var affected = await _repository.ConfirmLotSetupAsync(normalizedRequest, cancellationToken);
        var response = OperationResponseMapper.ToResponse(request, affected);

        return ApiResult<OperationResponse>.SuccessResult(
            response,
            _localizer["Success"]);
    }

    public async Task<ApiResult<OperationResponse>> RemoveLotSetupAsync(RemoveCarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(RemoveLotSetupAsync));
        var affected = await _repository.RemoveLotSetupAsync(request, cancellationToken);
        if (affected == 0)
        {
            return ApiResult<OperationResponse>.FailResult(_localizer["NoCarrierLotBinding"], 4004);
        }

        var response = OperationResponseMapper.ToResponse(request, affected);
        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
