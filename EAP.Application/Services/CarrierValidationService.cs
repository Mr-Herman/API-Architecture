using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;

namespace EAP.Application.Services;

public sealed class CarrierValidationService : ICarrierValidationService
{
    private readonly ICarrierQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public CarrierValidationService(ICarrierQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<ValidateLotCarrierResponse>> ValidateLotAsync(ValidateLotCarrierRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateLotAsync));
        var carrierId = request.CarrierId.Trim();
        var lotId = request.LotId.Trim();
        var canonicalLotId = await _repository.ResolveLotIdAsync(lotId, cancellationToken);

        if (string.IsNullOrWhiteSpace(canonicalLotId))
        {
            return ApiResult<ValidateLotCarrierResponse>.FailResult(_localizer["LotNotFound"], 4004);
        }

        if (await _repository.IsLotBoundAsync(canonicalLotId, cancellationToken))
        {
            return ApiResult<ValidateLotCarrierResponse>.FailResult(_localizer["LotAlreadyBound"], 4005);
        }

        var carrier = await _repository.GetSetupAsync(carrierId, cancellationToken);
        if (carrier is null)
        {
            return ApiResult<ValidateLotCarrierResponse>.FailResult(_localizer["CarrierNotFound"], 4004);
        }

        var response = CarrierValidationResponseMapper.ToResponse(carrier, canonicalLotId);
        return ApiResult<ValidateLotCarrierResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
