using EAP.Application.Interfaces.Commands;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;

namespace EAP.Application.Services.Commands;

public sealed class ToolCommandService : IToolCommandService
{
    private readonly IToolCommandRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public ToolCommandService(IToolCommandRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> ConfirmSetupAsync(ToolSetupRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ConfirmSetupAsync));
        var affected = await _repository.InsertSetupAsync(request, cancellationToken);
        var response = OperationResponseMapper.ToResponse(affected);

        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }

    public async Task<ApiResult<ToolVerificationResponse>> VerifyAsync(VerifyToolRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(VerifyAsync));
        var result = await _repository.VerifyAsync(request, cancellationToken);
        result.Message = _localizer[result.Message];
        return result.Passed
            ? ApiResult<ToolVerificationResponse>.SuccessResult(result, result.Message)
            : ApiResult<ToolVerificationResponse>.FailResult(result.Message, 4004);
    }
}
