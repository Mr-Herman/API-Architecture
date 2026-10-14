using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Carriers;
using EAP.Models.Responses.Carriers;

namespace EAP.Application.Services;

public sealed class CarrierQueryService : ICarrierQueryService
{
    private readonly ICarrierQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public CarrierQueryService(ICarrierQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<CarrierSetupResponse>> GetSetupAsync(GetCarrierSetupRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetSetupAsync));
        var result = await _repository.GetSetupAsync(request.CarrierId.Trim(), cancellationToken);
        if (result is null)
        {
            throw new ResourceNotFoundException(_localizer["CarrierNotFound"]);
        }
        var response = CarrierResponseMapper.ToResponse(result);
        return ApiResult<CarrierSetupResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
