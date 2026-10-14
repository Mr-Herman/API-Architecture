using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Equipment;

namespace EAP.DataAccess.Repositories;

public sealed class EquipmentQueryRepository : RepositoryBase, IEquipmentQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public EquipmentQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<EquipmentStatusEntity?> GetStatusAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                EQUIPMENT_ID AS EquipmentId,
                STATUS AS Status,
                SUB_STATUS AS SubStatus,
                SUB_STATUS_DESC AS SubStatusDesc
            FROM EQUIPMENT_STATUS_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
            ";

        return await QueryFirstOrDefaultAsync<EquipmentStatusEntity>(sql, new { EquipmentId = equipmentId }, cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentProcessEntity>> GetProcessesAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT DISTINCT
                t1.WORK_FLOW AS WorkFlow,
                t3.PRODUCT_NAME AS DisplayWorkFlow,
                t1.PROCESS AS Process,
                t4.PROCESS_ID AS DisplayProcess
            FROM PROCESS_EQUIPMENT_FAMILY_SETTING t1
            INNER JOIN EQUIPMENT_LIST t2 ON t1.EQUIPMENT_FAMILY = t2.EQUIPMENT_FAMILY
            INNER JOIN PRODUCT_INFO t3 ON t1.WORK_FLOW = t3.PRODUCT_CODE
            INNER JOIN PROCESS_INFO t4
                ON t1.WORK_FLOW = t4.PRODUCT
                AND t1.PROCESS = t4.PROCESS_NAME
            WHERE t2.EQUIPMENT_ID = :EquipmentId
              AND t2.STATUS = 'Active'
            ORDER BY t3.PRODUCT_NAME, t4.PROCESS_ID
            ";

        return await QueryListAsync<EquipmentProcessEntity>(
            sql,
            new { EquipmentId = equipmentId },
            cancellationToken);
    }
}
