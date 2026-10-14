using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Operators;

namespace EAP.DataAccess.Repositories;

public sealed class OperatorQueryRepository : RepositoryBase, IOperatorQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public OperatorQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<OperatorAccountEntity?> GetOperatorAccountAsync(string operatorId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                USER_ID AS OperatorId,
                PWD AS Password,
                USER_NAME AS OperatorName,
                ROLE_ID AS Roles
            FROM SYS_USER_LIST
            WHERE USER_ID = :OperatorId
            ";

        return QueryFirstOrDefaultAsync<OperatorAccountEntity>(
            sql,
            new { OperatorId = operatorId },
            cancellationToken);
    }

    public async Task<OperatorProcessEntity?> GetProcessOperatorsAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                WORK_FLOW AS WorkFlow,
                PROCESS AS Process,
                EMPLOYEE_ID AS OperatorId
            FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
            ORDER BY EMPLOYEE_ID
            ";

        var rows = await QueryListAsync<OperatorProcessRow>(
            sql,
            new { EquipmentId = equipmentId },
            cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        return new OperatorProcessEntity
        {
            WorkFlow = rows[0].WorkFlow,
            Process = rows[0].Process,
            Operators = rows
                .Select(row => row.OperatorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .ToArray()
        };
    }

    public async Task<bool> HasWorkInProgressAsync(string equipmentId, string operatorId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
              AND EMPLOYEE_ID = :OperatorId
            ";

        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { EquipmentId = equipmentId, OperatorId = operatorId },
            cancellationToken);

        return count > 0;
    }

    public async Task<ProcessDefinitionEntity?> GetProcessDefinitionAsync(string workFlow, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                a.PRODUCT AS Product,
                a.PROCESS_NAME AS ProcessName,
                a.PROCESS_CODE AS ProcessCode,
                b.PROJ_CODE AS ProjectCode
            FROM PROCESS_INFO a
            INNER JOIN PROJECT_LIST b ON a.PRODUCT = b.PRODUCT
            WHERE a.PRODUCT = :WorkFlow
              AND a.PROCESS_NAME = :Process
            ";

        var row = await QueryFirstOrDefaultAsync<ProcessDefinitionEntity>(
            sql,
            new { WorkFlow = workFlow, Process = process },
            cancellationToken);

        return row;
    }

    private sealed class OperatorProcessRow
    {
        public string? WorkFlow { get; set; }

        public string? Process { get; set; }

        public string? OperatorId { get; set; }
    }

}
