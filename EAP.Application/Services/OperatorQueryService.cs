using EAP.Application.Http;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Application.Localization;
using EAP.Models.Common;
using EAP.Models.Requests.Operators;
using EAP.Models.Responses.Operators;

namespace EAP.Application.Services;

public sealed class OperatorQueryService : IOperatorQueryService
{
    private readonly IOperatorQueryRepository _repository;
    private readonly IHttpApiClient _httpApiClient;
    private readonly IMessageLocalizer _localizer;

    public OperatorQueryService(
        IOperatorQueryRepository repository,
        IHttpApiClient httpApiClient,
        IMessageLocalizer localizer)
    {
        _repository = repository;
        _httpApiClient = httpApiClient;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperatorProcessResponse>> GetProcessOperatorsAsync(OperatorEquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetProcessOperatorsAsync));
        var result = await _repository.GetProcessOperatorsAsync(request.EquipmentId.Trim(), cancellationToken);
        if (result is null)
        {
            return ApiResult<OperatorProcessResponse>.FailResult(_localizer["NoOperatorProcessBinding"], 4004);
        }

        var response = OperatorResponseMapper.ToResponse(result);
        return ApiResult<OperatorProcessResponse>.SuccessResult(response, _localizer["Success"]);
    }

    public async Task<ApiResult<OperatorProcessValidationResponse>> ValidateProcessAsync(ValidateOperatorProcessRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateProcessAsync));
        var equipmentId = request.EquipmentId.Trim();
        var operatorId = request.OperatorId.Trim();
        var workFlow = request.WorkFlow.Trim();
        var process = request.Process.Trim();

        if (await _repository.HasWorkInProgressAsync(equipmentId, operatorId, cancellationToken))
        {
            return ApiResult<OperatorProcessValidationResponse>.SuccessResult(
                OperatorProcessValidationResponseMapper.ToResponse(
                    false,
                    _localizer["EquipmentOperatorBindingExists"]),
                _localizer["ProcessValidationFailed"]);
        }

        var definition = await _repository.GetProcessDefinitionAsync(workFlow, process, cancellationToken);
        var processCode = definition?.ProcessCode ?? "0";
        var projectCode = definition?.ProjectCode ?? "0";

        var response = await _httpApiClient.SendAsync(
            new HttpApiRequest("OaTrainingRecord")
            {
                Method = ApiHttpMethod.Get,
                QueryParameters = new Dictionary<string, string?>
                {
                    ["process_code"] = processCode,
                    ["proj_code"] = projectCode,
                    ["emp_code"] = operatorId
                }
            },
            cancellationToken);

        if (!response.IsSuccess)
        {
            return ApiResult<OperatorProcessValidationResponse>.SuccessResult(
                OperatorProcessValidationResponseMapper.ToResponse(
                    false,
                    _localizer["QualificationRequestFailed"]),
                _localizer["ProcessValidationFailed"]);
        }

        var qualification = OperatorAuthenticationResponseParser.ParseQualification(
            response.Content,
            _localizer);
        return ApiResult<OperatorProcessValidationResponse>.SuccessResult(
            qualification,
            qualification.Authenticated ? _localizer["Success"] : qualification.FailureReason ?? _localizer["ProcessValidationFailed"]);
    }
}
