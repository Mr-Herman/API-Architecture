using EAP.Application.Interfaces.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class NotificationCommandRepository : RepositoryBase, INotificationCommandRepository
{
    private const string DatabaseName = "DgFisOracle";

    public NotificationCommandRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<int> SendDashboardMessageAsync(DashboardMessageRequest request, CancellationToken cancellationToken)
    {
        string sql = @"
            INSERT INTO NOTIFICATION_MESSAGE
                (EQUIPMENT_ID, LOT_ID, SEVERITY, ALERT_TYPE, MESSAGE,
                 EVENT_TIME, OPERATOR_ID, WORK_FLOW, PROCESS)
            VALUES
                (:EquipmentId, :LotId, :Severity, :AlertType, :Message,
                 :EventTime, :OperatorId, :WorkFlow, :Process)
            ";

        return ExecuteAsync(
            sql,
            new
            {
                request.EquipmentId,
                request.LotId,
                request.Severity,
                request.AlertType,
                request.Message,
                EventTime = request.EventTime.DateTime,
                request.OperatorId,
                request.WorkFlow,
                request.Process
            },
            cancellationToken);
    }
}
