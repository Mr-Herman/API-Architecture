using EAP.Application.Interfaces.Commands;
using EAP.Application.Logging;
using EAP.Application.Localization;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services.Commands;

public sealed class EquipmentCommandService : IEquipmentCommandService
{
    private readonly IEquipmentCommandRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public EquipmentCommandService(IEquipmentCommandRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public Task<ApiResult<OperationResponse>> ConfirmOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(nameof(ConfirmOperatorAsync), request, _repository.InsertOperatorAsync, _localizer, cancellationToken);
    }

    public Task<ApiResult<OperationResponse>> RemoveOperatorAsync(RemoveOperatorBindingRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(nameof(RemoveOperatorAsync), request, _repository.RemoveOperatorAsync, _localizer, cancellationToken);
    }

    public Task<ApiResult<OperationResponse>> ChangeStatusAsync(EquipmentStatusChangeRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(nameof(ChangeStatusAsync), request, _repository.ChangeStatusAsync, _localizer, cancellationToken);
    }

    private static async Task<ApiResult<OperationResponse>> ExecuteAsync(
        string methodName,
        OperatorBindingRequest request,
        Func<OperatorBindingRequest, CancellationToken, Task<int>> action,
        IMessageLocalizer localizer,
        CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(methodName);
        var affected = await action(request, cancellationToken);
        if (affected == 0)
        {
            return ApiResult<OperationResponse>.FailResult(localizer["NoDatabaseRecordChanged"], 4004);
        }

        var response = OperationResponseMapper.ToResponse(affected);
        return ApiResult<OperationResponse>.SuccessResult(response, localizer["Success"]);
    }

    private static async Task<ApiResult<OperationResponse>> ExecuteAsync(
        string methodName,
        EquipmentStatusChangeRequest request,
        Func<EquipmentStatusChangeRequest, CancellationToken, Task<int>> action,
        IMessageLocalizer localizer,
        CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(methodName);
        var affected = await action(request, cancellationToken);
        var response = OperationResponseMapper.ToResponse(affected);

        return ApiResult<OperationResponse>.SuccessResult(response, localizer["Success"]);
    }

    private static async Task<ApiResult<OperationResponse>> ExecuteAsync(
        string methodName,
        RemoveOperatorBindingRequest request,
        Func<RemoveOperatorBindingRequest, CancellationToken, Task<int>> action,
        IMessageLocalizer localizer,
        CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(methodName);
        var affected = await action(request, cancellationToken);
        if (affected == 0)
        {
            return ApiResult<OperationResponse>.FailResult(localizer["NoDatabaseRecordChanged"], 4004);
        }

        var response = OperationResponseMapper.ToResponse(affected);
        return ApiResult<OperationResponse>.SuccessResult(response, localizer["Success"]);
    }
}
