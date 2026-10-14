using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Application.Localization;
using EAP.Models.Entities.Production;
using EAP.Models.Common;
using EAP.Models.Requests.Production;
using EAP.Models.Responses.Production;

namespace EAP.Application.Services;

public sealed class ProductionQueryService : IProductionQueryService
{
    private readonly IProductionQueryRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public ProductionQueryService(IProductionQueryRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<IReadOnlyList<ProcessDefectResponse>>> GetProcessDefectsAsync(ProcessDefectRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetProcessDefectsAsync));
        var result = await _repository.GetProcessDefectsAsync(request.WorkFlow.Trim(), request.Process.Trim(), cancellationToken);
        var responses = result.Select(ProductionResponseMapper.ToResponse).ToArray();
        return ApiResult<IReadOnlyList<ProcessDefectResponse>>.SuccessResult(responses, _localizer["Success"]);
    }

    public async Task<ApiResult<LotInformationResponse>> GetLotInformationAsync(LotInformationRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(GetLotInformationAsync));
        var equipmentId = request.EquipmentId.Trim();
        var workFlow = Normalize(request.WorkFlow);
        var process = Normalize(request.Process);
        var operation = await _repository.GetEquipmentOperationAsync(equipmentId, workFlow, process, cancellationToken);

        if (operation is null)
        {
            throw new ResourceNotFoundException(_localizer["EquipmentProcessNotFound"]);
        }

        if (operation.TraceWay is not (1 or 2))
        {
            throw new ResourceNotFoundException($"{_localizer["WorkflowNotSupported"]}{operation.WorkFlow}.");
        }

        var lotRows = operation.TraceWay == 2
            ? await _repository.GetLotRowsAsync(operation.WorkFlow ?? string.Empty, request.LotIdOrSn.Trim(), cancellationToken)
            : Array.Empty<ProductionLotEntity>();

        var lot = operation.TraceWay == 2
            ? lotRows.FirstOrDefault()
            : await _repository.GetLotMasterAsync(request.LotIdOrSn.Trim(), cancellationToken);

        if (lot is null)
        {
            throw new ResourceNotFoundException(_localizer["LotOrSerialNotFound"]);
        }

        if (string.Equals(lot.IsFinished, "Y", StringComparison.OrdinalIgnoreCase))
        {
            throw new ResourceNotFoundException(_localizer["LotCompleted"]);
        }

        var serialNumbers = operation.TraceWay == 2
            ? lotRows.Select(row => row.SerialNumber).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray()
            : Array.Empty<string>();

        var recipe = await _repository.GetRecipeAsync(
            lot.ProjectCode ?? string.Empty,
            operation.WorkFlow ?? string.Empty,
            operation.Process ?? string.Empty,
            lot.FormNo ?? string.Empty,
            cancellationToken);

        if (recipe is null)
        {
            throw new ResourceNotFoundException(_localizer["RecipeNotFound"]);
        }

        var tools = await _repository.GetToolRequirementsAsync(
            lot.ProductPartNumber ?? string.Empty,
            operation.WorkFlow ?? string.Empty,
            operation.Process ?? string.Empty,
            equipmentId,
            cancellationToken);

        var materials = await _repository.GetMaterialRequirementsAsync(
            lot.ProductPartNumber ?? string.Empty,
            operation.WorkFlow ?? string.Empty,
            operation.Process ?? string.Empty,
            cancellationToken);

        var result = ProductionResponseMapper.ToLotInformationResponse(
            operation,
            lot,
            recipe,
            request.LotIdOrSn.Trim(),
            serialNumbers,
            tools,
            materials);

        return ApiResult<LotInformationResponse>.SuccessResult(result, _localizer["Success"]);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
