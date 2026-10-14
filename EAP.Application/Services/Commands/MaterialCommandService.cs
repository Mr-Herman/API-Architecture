using EAP.Application.Interfaces.Commands;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services.Commands;

public sealed class MaterialCommandService : IMaterialCommandService
{
    private readonly IMaterialCommandRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public MaterialCommandService(IMaterialCommandRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> ConfirmSetupAsync(MaterialSetupRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ConfirmSetupAsync));
        var affected = await _repository.ReplaceSetupAsync(request, cancellationToken);
        var response = OperationResponseMapper.ToResponse(request, affected);

        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
