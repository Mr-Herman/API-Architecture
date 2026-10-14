using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Quality;
using EAP.Models.Responses.Quality;

namespace EAP.Application.Services;

public sealed class QualityInspectionService : IQualityInspectionService
{
    private const string FaiInspectionType = "FAI";
    private const string SpcInspectionType = "PI";

    private readonly IQaInspectionRepository _qaRepository;
    private readonly ISpcInspectionRepository _spcRepository;
    private readonly IMessageLocalizer _localizer;

    public QualityInspectionService(IQaInspectionRepository qaRepository, ISpcInspectionRepository spcRepository, IMessageLocalizer localizer)
    {
        _qaRepository = qaRepository;
        _spcRepository = spcRepository;
        _localizer = localizer;
    }

    public Task<ApiResult<QualityInspectionResponse>> ValidateInspectionAsync(QualityInspectionRequest request, CancellationToken cancellationToken)
    {
        var inspectionType = request.InspectType.Trim().ToUpperInvariant();

        if (inspectionType == FaiInspectionType)
        {
            return ValidateFaiAsync(request, cancellationToken);
        }

        if (inspectionType == SpcInspectionType)
        {
            return ValidateSpcAsync(request, cancellationToken);
        }

        return Task.FromResult(CreateResult(request, inspectionType, false, _localizer["QualityInspectionTypeInvalid"], DateTimeOffset.Now));
    }

    private async Task<ApiResult<QualityInspectionResponse>> ValidateFaiAsync(QualityInspectionRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateFaiAsync));
        var startTime = DateTimeOffset.Now;
        var equipmentId = request.EquipmentId.Trim();
        var processCode = await _qaRepository.GetProcessCodeAsync(request.Project.Trim(), request.ProcessCode.Trim(), cancellationToken);

        if (string.IsNullOrWhiteSpace(processCode))
        {
            return CreateResult(request, FaiInspectionType, false, _localizer["QualityFaiProcessMappingNotFound"], startTime);
        }

        var configured = await _qaRepository.IsEquipmentConfiguredAsync(processCode, equipmentId, cancellationToken);
        if (!configured)
        {
            return CreateResult(
                request,
                FaiInspectionType,
                false,
                _localizer["QualityFaiEquipmentNotConfigured"],
                startTime);
        }

        var inspections = await _qaRepository.GetLatestInspectionsAsync(equipmentId, processCode, cancellationToken);
        var passed = inspections.Count > 0
            && inspections.All(item => string.Equals(item.InspectionResult, "Pass", StringComparison.OrdinalIgnoreCase));
        var message = passed
            ? _localizer["QualityFaiValidationPassed"]
            : _localizer["QualityFaiValidationFailed"];

        return CreateResult(request, FaiInspectionType, passed, message, startTime);
    }

    private async Task<ApiResult<QualityInspectionResponse>> ValidateSpcAsync(QualityInspectionRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(ValidateSpcAsync));
        var startTime = DateTimeOffset.Now;
        var equipmentId = request.EquipmentId.Trim();
        var processCode = await _spcRepository.GetProcessCodeAsync(request.Project.Trim(), request.ProcessCode.Trim(), cancellationToken);

        if (string.IsNullOrWhiteSpace(processCode))
        {
            return CreateResult(request, SpcInspectionType, false, _localizer["QualitySpcProcessMappingNotFound"], startTime);
        }

        var configured = await _spcRepository.IsEquipmentConfiguredAsync(processCode, equipmentId, cancellationToken);
        if (!configured)
        {
            return CreateResult(request, SpcInspectionType, false, _localizer["QualitySpcEquipmentNotConfigured"], startTime);
        }

        var inspection = await _spcRepository.GetInspectionAsync(equipmentId, processCode, cancellationToken);
        if (inspection is null)
        {
            return CreateResult(request, SpcInspectionType, false, _localizer["QualitySpcDataNotFound"], startTime);
        }

        var passed = string.Equals(inspection.Result, "PASS", StringComparison.OrdinalIgnoreCase);
        var message = passed
            ? _localizer["QualitySpcValidationPassed"]
            : _localizer["QualitySpcValidationFailed"];
        var reason = passed || string.IsNullOrWhiteSpace(inspection.ProblemMessage)
            ? message
            : inspection.ProblemMessage;

        return CreateResult(request, SpcInspectionType, passed, reason, startTime, message);
    }

    private ApiResult<QualityInspectionResponse> CreateResult(QualityInspectionRequest request, string inspectionType, bool passed, string reason, DateTimeOffset startTime, string? message = null)
    {
        var response = QualityInspectionResponseMapper.ToResponse(passed, inspectionType, request.EquipmentId.Trim(), passed ? null : reason, startTime);

        return ApiResult<QualityInspectionResponse>.SuccessResult(response, message ?? reason);
    }
}
