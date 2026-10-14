using EAP.Application.Interfaces.Commands;
using EAP.Application.Localization;
using EAP.Application.Logging;
using EAP.Application.Mappings;
using EAP.Models.Common;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Services.Commands;

public sealed class NotificationCommandService : INotificationCommandService
{
    private readonly INotificationCommandRepository _repository;
    private readonly IMessageLocalizer _localizer;

    public NotificationCommandService(INotificationCommandRepository repository, IMessageLocalizer localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    public async Task<ApiResult<OperationResponse>> SendDashboardMessageAsync(DashboardMessageRequest request, CancellationToken cancellationToken)
    {
        using var timing = MethodTimingScope.Start(nameof(SendDashboardMessageAsync));
        var affected = await _repository.SendDashboardMessageAsync(request, cancellationToken);
        var response = OperationResponseMapper.ToResponse(affected);

        return ApiResult<OperationResponse>.SuccessResult(response, _localizer["Success"]);
    }
}
