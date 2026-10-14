using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Constants;
using EAP.Models.Entities.Tracking;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services;

public sealed class ProductionTrackingService : IProductionTrackingService
{
    private readonly IProductionTrackingRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public ProductionTrackingService(IProductionTrackingRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> TrackInAsync(TrackInRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(TrackInAsync));
        Normalize(request);

        if (await _repository.IsTransactionCompletedAsync(request.TransactionId, cancellationToken))
        {
            return Success(request, 0, _localizer["TransactionAlreadyCompleted"]);
        }

        var context = await _repository.GetTrackInContextAsync(request, cancellationToken);
        if (context is null)
        {
            return Failure(_localizer["TrackingContextNotFound"]);
        }

        if (context.TraceWay != request.TraceWay)
        {
            return Failure(_localizer["TrackingTraceWayInvalid"]);
        }

        var materials = await _repository.GetMaterialsAsync(
            context.ProductPartNumber ?? string.Empty,
            context.WorkFlow,
            context.Process,
            request.EquipmentId,
            cancellationToken);
        var materialValidationMessage = ValidateMaterials(
            materials,
            string.IsNullOrWhiteSpace(request.SerialNumber) ? context.ProcessQuantity : 1);
        if (materialValidationMessage is not null)
        {
            return Failure(materialValidationMessage);
        }

        var affected = await _repository.ExecuteTrackInAsync(request, context, cancellationToken);
        return affected > 0
            ? Success(request, affected, _localizer["TrackInSucceeded"])
            : Failure(_localizer["NoDatabaseRecordChanged"]);
    }

    public async Task<ApiResult<OperationResponse>> TrackOutAsync(TrackOutRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(TrackOutAsync));
        Normalize(request);

        if (await _repository.IsTransactionCompletedAsync(request.TransactionId, cancellationToken))
        {
            return Success(request, 0, _localizer["TransactionAlreadyCompleted"]);
        }

        var context = await _repository.GetTrackOutContextAsync(request, cancellationToken);
        if (context is null)
        {
            return Failure(_localizer["TrackingContextNotFound"]);
        }

        if (context.TraceWay != request.TraceWay)
        {
            return Failure(_localizer["TrackingTraceWayInvalid"]);
        }

        var lotWip = await _repository.GetLotWipAsync(request.LotId, cancellationToken);
        if (lotWip is null)
        {
            return Failure(_localizer["TrackInDataNotFound"]);
        }

        if (lotWip.Status != 1)
        {
            return Failure(_localizer["LotAlreadyTrackedOut"]);
        }

        if (!string.Equals(lotWip.WorkFlow, context.WorkFlow, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(lotWip.Process, context.Process, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(_localizer["TrackOutProcessMismatch"]);
        }

        var snWips = request.TraceWay == 2
            ? await _repository.GetSnWipsAsync(request.LotId, cancellationToken)
            : [];
        var defectCodes = await _repository.GetDefectCodesAsync(
            context.WorkFlow,
            context.Process,
            cancellationToken);
        var validationMessage = ValidateTrackOut(
            request,
            context,
            lotWip.TrackInQuantity,
            snWips,
            defectCodes);
        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        var materials = await _repository.GetMaterialsAsync(
            lotWip.ProductPartNumber ?? string.Empty,
            context.WorkFlow,
            context.Process,
            request.EquipmentId,
            cancellationToken);
        var trackOutQuantity = request.TraceWay == 1
            ? request.TrackOutLotResultList.Sum(item => item.Quantity)
            : request.TrackOutSnResultList.Count;
        var materialValidationMessage = ValidateMaterials(materials, trackOutQuantity);
        if (materialValidationMessage is not null)
        {
            return Failure(materialValidationMessage);
        }

        var affected = await _repository.ExecuteTrackOutAsync(
            request,
            context,
            lotWip,
            snWips,
            materials,
            cancellationToken);

        return affected > 0
            ? Success(request, affected, _localizer["TrackOutSucceeded"])
            : Failure(_localizer["NoDatabaseRecordChanged"]);
    }

    private string? ValidateTrackOut(
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        int trackInQuantity,
        IReadOnlyList<ProductionSnWipEntity> snWips,
        IReadOnlySet<string> defectCodes)
    {
        if (request.TraceWay == 1)
        {
            if (request.TrackOutLotResultList.Count == 0
                || request.TrackOutLotResultList.Sum(item => item.Quantity) != trackInQuantity)
            {
                return _localizer["TrackOutQuantityMismatch"];
            }

            foreach (var item in request.TrackOutLotResultList)
            {
                var result = ValidateResult(item.ProcessResult, item.DefectCode, defectCodes);
                if (result is not null)
                {
                    return result;
                }
            }

            return null;
        }

        if (request.TrackOutSnResultList.Count == 0)
        {
            return _localizer["TrackOutResultRequired"];
        }

        foreach (var item in request.TrackOutSnResultList)
        {
            var wip = snWips.FirstOrDefault(row => string.Equals(
                row.SerialNumber,
                item.SerialNumber,
                StringComparison.OrdinalIgnoreCase));
            if (wip is null || wip.Status != 1)
            {
                return _localizer["SnNotTrackedIn"];
            }

            if (!string.Equals(wip.Tester, request.EquipmentId, StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["TrackOutEquipmentMismatch"];
            }

            if (!string.Equals(wip.WorkFlow, context.WorkFlow, StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["TrackOutProcessMismatch"];
            }

            if (!string.Equals(wip.Process, context.Process, StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["TrackOutProcessMismatch"];
            }

            var result = ValidateResult(item.ProcessResult, item.DefectCode, defectCodes);
            if (result is not null)
            {
                return result;
            }

            if (IsPass(item.ProcessResult) && item.OnlineRework == 1)
            {
                return _localizer["PassCannotOnlineRework"];
            }
        }

        return null;
    }

    private string? ValidateResult(string processResult, string? defectCode, IReadOnlySet<string> defectCodes)
    {
        if (!IsPass(processResult) && !IsFail(processResult))
        {
            return _localizer["ProcessResultInvalid"];
        }

        if (IsPass(processResult) && !string.IsNullOrWhiteSpace(defectCode))
        {
            return _localizer["PassCannotContainDefect"];
        }

        if (IsFail(processResult)
            && (string.IsNullOrWhiteSpace(defectCode) || !defectCodes.Contains(defectCode)))
        {
            return _localizer["FailRequiresValidDefect"];
        }

        return null;
    }

    private string? ValidateMaterials(
        IReadOnlyList<ProductionMaterialEntity> materials,
        int productionQuantity)
    {
        if (materials.Count == 0)
        {
            return null;
        }

        if (materials[0].RequiredComponentCount != materials[0].LoadedComponentCount)
        {
            return _localizer["MaterialComponentCountMismatch"];
        }

        foreach (var component in materials.GroupBy(
                     material => material.ComponentName,
                     StringComparer.OrdinalIgnoreCase))
        {
            var material = component.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.MaterialLot));
            if (material is null)
            {
                return _localizer["RequiredMaterialNotLoaded"];
            }

            if (material.ExpireTime.HasValue && material.ExpireTime.Value <= DateTime.Now)
            {
                return _localizer["LoadedMaterialExpired"];
            }

            if (material.RemainingQuantity < productionQuantity * material.ConsumptionFactor)
            {
                return _localizer["LoadedMaterialInsufficient"];
            }
        }

        return null;
    }

    private static bool IsPass(string value)
    {
        return string.Equals(value, "PASS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFail(string value)
    {
        return string.Equals(value, "FAIL", StringComparison.OrdinalIgnoreCase);
    }

    private static void Normalize(TrackInRequest request)
    {
        request.TransactionId = request.TransactionId.Trim();
        request.EquipmentId = request.EquipmentId.Trim();
        request.RecipeName = request.RecipeName.Trim();
        request.LotId = request.LotId?.Trim();
        request.SerialNumber = request.SerialNumber?.Trim();
        request.WorkFlow = request.WorkFlow?.Trim();
        request.Process = request.Process?.Trim();
        request.EmployeeId = request.EmployeeId?.Trim();
        if (request.TraceWay == 1 && string.IsNullOrWhiteSpace(request.LotId))
        {
            request.LotId = request.SerialNumber;
            request.SerialNumber = null;
        }
    }

    private static void Normalize(TrackOutRequest request)
    {
        request.TransactionId = request.TransactionId.Trim();
        request.LotId = request.LotId.Trim();
        request.EquipmentId = request.EquipmentId.Trim();
        request.WorkFlow = request.WorkFlow?.Trim();
        request.Process = request.Process?.Trim();
        request.EmployeeId = request.EmployeeId?.Trim();
    }

    private ApiResult<OperationResponse> Success(ITransactionRequest request, int affected, string message)
    {
        var response = OperationResponseMapper.ToResponse(request, affected);
        return ApiResult<OperationResponse>.SuccessResult(response, message);
    }

    private static ApiResult<OperationResponse> Failure(string message)
    {
        return ApiResult<OperationResponse>.FailResult(message, BusinessCode.ParamError);
    }
}
