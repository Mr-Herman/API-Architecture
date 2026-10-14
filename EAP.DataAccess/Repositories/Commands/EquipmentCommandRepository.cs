using Dapper;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class EquipmentCommandRepository : RepositoryBase, IEquipmentCommandRepository
{
    private const string DatabaseName = "DgFisOracle";

    public EquipmentCommandRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<int> UpsertOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken)
    {
        string sql = @"
            MERGE INTO EQUIPMENT_PROCESS_EMPLOYEE_WIP target
            USING (
                SELECT
                    :EquipmentId AS EQUIPMENT_ID,
                    :OperatorId AS EMPLOYEE_ID,
                    :WorkFlow AS WORK_FLOW,
                    :Process AS PROCESS
                FROM DUAL
            ) source
            ON (target.EQUIPMENT_ID = source.EQUIPMENT_ID)
            WHEN MATCHED THEN
                UPDATE SET
                    target.EMPLOYEE_ID = source.EMPLOYEE_ID,
                    target.WORK_FLOW = source.WORK_FLOW,
                    target.PROCESS = source.PROCESS
            WHEN NOT MATCHED THEN
                INSERT (EQUIPMENT_ID, EMPLOYEE_ID, WORK_FLOW, PROCESS)
                VALUES (source.EQUIPMENT_ID, source.EMPLOYEE_ID, source.WORK_FLOW, source.PROCESS)
            ";

        return ExecuteAsync(sql, request, cancellationToken);
    }

    public Task<int> InsertOperatorAsync(OperatorBindingRequest request, CancellationToken cancellationToken)
    {
        string sql = @"
            INSERT INTO EQUIPMENT_PROCESS_EMPLOYEE_WIP
                (EQUIPMENT_ID, EMPLOYEE_ID, WORK_FLOW, PROCESS)
            VALUES
                (:EquipmentId, :OperatorId, :WorkFlow, :Process)
            ";

        return ExecuteAsync(sql, request, cancellationToken);
    }

    public Task<int> RemoveOperatorAsync(RemoveOperatorBindingRequest request, CancellationToken cancellationToken)
    {
        string sql = @"
            DELETE FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
              AND EMPLOYEE_ID = :OperatorId
            ";

        return ExecuteAsync(sql, request, cancellationToken);
    }

    public Task<int> ChangeStatusAsync(EquipmentStatusChangeRequest request, CancellationToken cancellationToken)
    {
        string deleteSql = "DELETE FROM EQUIPMENT_STATUS_WIP WHERE EQUIPMENT_ID = :EquipmentId";
        string insertWipSql = @"
            INSERT INTO EQUIPMENT_STATUS_WIP
                (EQUIPMENT_ID, STATUS, SUB_STATUS, SUB_STATUS_DESC)
            VALUES
                (:EquipmentId, :Status, :SubStatus, :SubStatusDesc)
            ";
        string insertHistorySql = @"
            INSERT INTO EQUIPMENT_STATUS_HIS
                (EQUIPMENT_ID, STATUS, SUB_STATUS, SUB_STATUS_DESC)
            VALUES
                (:EquipmentId, :Status, :SubStatus, :SubStatusDesc)
            ";

        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                var parameters = new
                {
                    request.EquipmentId,
                    request.Status,
                    request.SubStatus,
                    request.SubStatusDesc
                };
                var deleted = await connection.ExecuteAsync(CreateCommand(deleteSql, parameters, token, transaction));
                var inserted = await connection.ExecuteAsync(CreateCommand(insertWipSql, parameters, token, transaction));
                await connection.ExecuteAsync(CreateCommand(insertHistorySql, parameters, token, transaction));
                return deleted + inserted;
            },
            cancellationToken);
    }
}
