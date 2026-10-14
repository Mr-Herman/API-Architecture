using EAP.Application.Exceptions;
using EAP.Application.Interfaces;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.ProcessAssemblies;
using EAP.Models.Responses.ProcessAssemblies;

namespace EAP.Application.Services;

public sealed class ProcessAssemblyService : IProcessAssemblyService
{
    private readonly IProcessAssemblyRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public ProcessAssemblyService(IProcessAssemblyRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<PagedResult<ProcessAssemblyResponse>>> SearchAsync(ProcessAssemblyQueryRequest request, CancellationToken cancellationToken)
    {
        using var timing = StartTiming(nameof(SearchAsync));
        ValidateSearchRequest(request);
        NormalizeSearchRequest(request);

        var result = await _repository.SearchAsync(request, cancellationToken);
        var items = result.Items.Select(ProcessAssemblyMapper.ToResponse).ToArray();
        var page = new PagedResult<ProcessAssemblyResponse>
        {
            Items = items,
            TotalCount = result.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ApiResult<PagedResult<ProcessAssemblyResponse>>.SuccessResult(page, _localizer["Success"]);
    }

    public async Task<ApiResult<ProcessAssemblyResponse>> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using var timing = StartTiming(nameof(GetByIdAsync));

        var details = await _repository.GetDetailsByIdAsync(id, cancellationToken);
        if (details is null)
        {
            throw new ResourceNotFoundException(_localizer["ProcessAssemblyNotFound"]);
        }

        var response = ProcessAssemblyMapper.ToResponse(details);
        return ApiResult<ProcessAssemblyResponse>.SuccessResult(response, _localizer["Success"]);
    }

    public async Task<ApiResult<ProcessAssemblyResponse>> CreateAsync(CreateProcessAssemblyRequest request, CancellationToken cancellationToken)
    {
        using var timing = StartTiming(nameof(CreateAsync));
        ValidateWriteRequest(request);

        var model = ProcessAssemblyMapper.ToEntity(request);
        model.Id = await _repository.CreateAsync(model, cancellationToken);
        var response = ProcessAssemblyMapper.ToResponse(model);
        return ApiResult<ProcessAssemblyResponse>.SuccessResult(response, _localizer["Created"]);
    }

    public async Task<ApiResult<ProcessAssemblyResponse>> UpdateAsync(long id, UpdateProcessAssemblyRequest request, CancellationToken cancellationToken)
    {
        using var timing = StartTiming(nameof(UpdateAsync));
        ValidateWriteRequest(request);

        var model = ProcessAssemblyMapper.ToEntity(request);
        model.Id = id;

        var updated = await _repository.UpdateAsync(model, cancellationToken);
        if (!updated)
        {
            throw new ResourceNotFoundException(_localizer["ProcessAssemblyNotFound"]);
        }

        var response = ProcessAssemblyMapper.ToResponse(model);
        return ApiResult<ProcessAssemblyResponse>.SuccessResult(response, _localizer["Updated"]);
    }

    public async Task<ApiResult<object>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        using var timing = StartTiming(nameof(DeleteAsync));

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new ResourceNotFoundException(_localizer["ProcessAssemblyNotFound"]);
        }

        return ApiResult<object>.SuccessResult(null, _localizer["Deleted"]);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void NormalizeSearchRequest(ProcessAssemblyQueryRequest request)
    {
        request.JobNum = Normalize(request.JobNum);
        request.PartNo = Normalize(request.PartNo);
        request.ProcessId = Normalize(request.ProcessId);
        request.ProductionLotNo = Normalize(request.ProductionLotNo);
        request.OperatorId = Normalize(request.OperatorId);
    }

    private void ValidateWriteRequest(ProcessAssemblyWriteRequest request)
    {
        if (request.StartTime.HasValue
            && request.TestTime.HasValue
            && request.TestTime < request.StartTime)
        {
            throw new RequestValidationException(_localizer["StartTimeAfterTestTime"]);
        }

        if (request.InputQty < request.OutputQty)
        {
            throw new RequestValidationException(_localizer["InputQuantityLessThanOutputQuantity"]);
        }
    }

    private void ValidateSearchRequest(ProcessAssemblyQueryRequest request)
    {
        if (request.StartTimeFrom.HasValue
            && request.StartTimeTo.HasValue
            && request.StartTimeTo < request.StartTimeFrom)
        {
            throw new RequestValidationException(_localizer["StartTimeRangeInvalid"]);
        }
    }

    private static MethodTimingScope StartTiming(string methodName)
    {
        return MethodTimingScope.Start(methodName);
    }
}
