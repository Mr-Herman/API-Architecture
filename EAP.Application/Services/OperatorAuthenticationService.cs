using EAP.Application.Http;
using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Entities.Operators;
using EAP.Models.Requests.Operators;
using EAP.Models.Responses.Operators;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services;

public sealed class OperatorAuthenticationService : IOperatorAuthenticationService
{
    private readonly IOperatorQueryRepository _repository;
    private readonly IEquipmentCommandRepository _commandRepository;
    private readonly IHttpApiClient _httpApiClient;
    private readonly ILegacyPasswordEncoder _passwordEncoder;
    private readonly IMessageLocalizer _localizer;

    public OperatorAuthenticationService(
        IOperatorQueryRepository repository,
        IEquipmentCommandRepository commandRepository,
        IHttpApiClient httpApiClient,
        ILegacyPasswordEncoder passwordEncoder,
        IMessageLocalizer localizer)
    {
        _repository = repository;
        _commandRepository = commandRepository;
        _httpApiClient = httpApiClient;
        _passwordEncoder = passwordEncoder;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperatorAuthenticationResponse>> AuthenticateAsync(AuthenticateOperatorRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(AuthenticateAsync));

        var credentials = await ValidateCredentialsAsync(OperatorAuthenticationRequestMapper.ToCredentialsRequest(request), cancellationToken);
        if (credentials.Data?.Authenticated != true)
        {
            return credentials;
        }

        var equipment = await ValidateEquipmentAvailabilityAsync(OperatorAuthenticationRequestMapper.ToEquipmentRequest(request), cancellationToken);
        if (equipment.Data?.Available != true)
        {
            return AuthenticationFailedFromResponse(credentials.Data, equipment.Message);
        }

        var qualification = await ValidateQualificationAsync(OperatorAuthenticationRequestMapper.ToQualificationRequest(request), cancellationToken);
        if (qualification.Data?.Authenticated != true)
        {
            return AuthenticationFailedFromResponse(credentials.Data, qualification.Message);
        }

        var login = await LoginOperatorAsync(OperatorAuthenticationRequestMapper.ToLoginRequest(request), cancellationToken);
        if (!login.Success)
        {
            return AuthenticationFailedFromResponse(credentials.Data, login.Message);
        }

        return ApiResult<OperatorAuthenticationResponse>.SuccessResult(credentials.Data, _localizer["Success"]);
    }

    public async Task<ApiResult<OperatorAuthenticationResponse>> ValidateCredentialsAsync(ValidateOperatorCredentialsRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateCredentialsAsync));

        var operatorId = request.OperatorId.Trim();
        var account = await _repository.GetOperatorAccountAsync(operatorId, cancellationToken);
        if (account is null)
        {
            return AuthenticationFailed(
                (OperatorAccountEntity?)null,
                _localizer["OperatorNotFound"]);
        }

        var encodedPassword = _passwordEncoder.Encode(request.Password);
        if (!string.Equals(account.Password, encodedPassword, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticationFailed(account, _localizer["OperatorPasswordIncorrect"]);
        }

        var response = OperatorAuthenticationResponseMapper.ToResponse(account, true, null);
        return ApiResult<OperatorAuthenticationResponse>.SuccessResult(response, _localizer["Success"]);
    }

    public async Task<ApiResult<EquipmentAvailabilityResponse>> ValidateEquipmentAvailabilityAsync(ValidateEquipmentAvailabilityRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateEquipmentAvailabilityAsync));

        var apiResponse = await _httpApiClient.SendAsync(
            new HttpApiRequest("OaEquipmentInfoBySn")
            {
                Method = ApiHttpMethod.Get,
                QueryParameters = new Dictionary<string, string?>
                {
                    ["SN"] = request.EquipmentId.Trim()
                }
            },
            cancellationToken);

        var available = apiResponse.IsSuccess
            && OperatorAuthenticationResponseParser.IsEquipmentAvailable(apiResponse.Content);
        var message = available ? _localizer["Success"] : _localizer["EquipmentNotAvailable"];
        var response = EquipmentAvailabilityResponseMapper.ToResponse(
            available,
            available ? null : message);

        return ApiResult<EquipmentAvailabilityResponse>.SuccessResult(response, message);
    }

    public async Task<ApiResult<OperatorProcessValidationResponse>> ValidateQualificationAsync(ValidateOperatorQualificationRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateQualificationAsync));

        var definition = await _repository.GetProcessDefinitionAsync(
            request.WorkFlow.Trim(),
            request.Process.Trim(),
            cancellationToken);
        var apiResponse = await _httpApiClient.SendAsync(
            new HttpApiRequest("OaTrainingRecord")
            {
                Method = ApiHttpMethod.Get,
                QueryParameters = new Dictionary<string, string?>
                {
                    ["process_code"] = definition?.ProcessCode ?? "0",
                    ["proj_code"] = definition?.ProjectCode ?? "0",
                    ["emp_code"] = request.OperatorId.Trim()
                }
            },
            cancellationToken);

        if (!apiResponse.IsSuccess)
        {
            var failed = OperatorProcessValidationResponseMapper.ToResponse(false, _localizer["QualificationRequestFailed"]);
            return ApiResult<OperatorProcessValidationResponse>.SuccessResult(failed, _localizer["QualificationRequestFailed"]);
        }

        var response = OperatorAuthenticationResponseParser.ParseQualification(apiResponse.Content, _localizer);
        var message = response.Authenticated ? _localizer["Success"] : response.FailureReason ?? _localizer["OperatorNotQualified"];

        return ApiResult<OperatorProcessValidationResponse>.SuccessResult(response, message);
    }

    public async Task<ApiResult<OperationResponse>> LoginOperatorAsync(LoginOperatorRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(LoginOperatorAsync));

        var affected = await _commandRepository.UpsertOperatorAsync(
            OperatorAuthenticationRequestMapper.ToBindingRequest(request),
            cancellationToken);
        if (affected == 0)
        {
            return ApiResult<OperationResponse>.FailResult(_localizer["NoDatabaseRecordChanged"], 4004);
        }

        var response = OperationResponseMapper.ToResponse(affected);
        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }

    private ApiResult<OperatorAuthenticationResponse> AuthenticationFailed(OperatorAccountEntity? account, string message)
    {
        var response = OperatorAuthenticationResponseMapper.ToResponse(account, false, message);
        return ApiResult<OperatorAuthenticationResponse>.SuccessResult(response, message);
    }

    private ApiResult<OperatorAuthenticationResponse> AuthenticationFailedFromResponse(OperatorAuthenticationResponse account, string message)
    {
        var response = OperatorAuthenticationResponseMapper.ToResponse(account, false, message);
        return ApiResult<OperatorAuthenticationResponse>.SuccessResult(response, message);
    }
}
