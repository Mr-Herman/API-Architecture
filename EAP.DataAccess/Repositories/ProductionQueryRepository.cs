using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Production;

namespace EAP.DataAccess.Repositories;

public sealed class ProductionQueryRepository : RepositoryBase, IProductionQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public ProductionQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<ProductionEquipmentOperationEntity?> GetEquipmentOperationAsync(string equipmentId, string? workFlow, string? process, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workFlow) || string.IsNullOrWhiteSpace(process))
        {
            string equipmentSql = @"
                SELECT
                    t1.WORK_FLOW AS WorkFlow,
                    t1.PROCESS AS Process,
                    t2.PROCESS_CODE AS ProcessCode,
                    t3.LOT_CONTROL AS TraceWay,
                    t2.PROCESS_LEAD_TIME AS ProcessLeadTime,
                    t2.ONLINE_REWORK AS OnlineRework
                FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP t1
                INNER JOIN PROCESS_INFO t2
                    ON t1.WORK_FLOW = t2.PRODUCT
                    AND t1.PROCESS = t2.PROCESS_NAME
                INNER JOIN PRODUCT_INFO t3 ON t1.WORK_FLOW = t3.PRODUCT_CODE
                WHERE t1.EQUIPMENT_ID = :EquipmentId
                ";

            return MapOperationAsync(
                equipmentSql,
                new { EquipmentId = equipmentId },
                cancellationToken);
        }

        string processSql = @"
            SELECT
                t1.PRODUCT AS WorkFlow,
                t1.PROCESS_NAME AS Process,
                t1.PROCESS_CODE AS ProcessCode,
                t2.LOT_CONTROL AS TraceWay,
                t1.PROCESS_LEAD_TIME AS ProcessLeadTime,
                t1.ONLINE_REWORK AS OnlineRework
            FROM PROCESS_INFO t1
            INNER JOIN PRODUCT_INFO t2 ON t1.PRODUCT = t2.PRODUCT_CODE
            WHERE t1.PRODUCT = :WorkFlow
              AND t1.PROCESS_NAME = :Process
            ";

        return MapOperationAsync(
            processSql,
            new { WorkFlow = workFlow, Process = process },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionLotEntity>> GetLotRowsAsync(string workFlow, string lotIdOrSn, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                t1.PRODUCTION_LOT_NO AS LotId,
                t2.JOB_NUM AS WorkOrder,
                t2.PART_NO AS ProductPartNumber,
                t2.IS_FINISHED AS IsFinished,
                t2.FORM_NO AS FormNo,
                t2.PROJ_CODE AS ProjectCode,
                t2.PRODUCT AS WorkFlow,
                t2.PROCESS_QTY AS ProcessQuantity,
                t2.MARKING AS LotMarking,
                t1.KEY_MATERIAL_SN AS SerialNumber
            FROM SN_MASTER t1
            INNER JOIN PRODUCTION_LOT_MASTER t2 ON t1.PRODUCTION_LOT_NO = t2.PRODUCTION_LOT_NO
            WHERE (t1.PRODUCTION_LOT_NO = :LotIdOrSn OR t1.KEY_MATERIAL_SN = :LotIdOrSn)
              AND t2.PRODUCT = :WorkFlow
              AND t1.KEY_MATERIAL_SN IS NOT NULL
              AND t1.DEFECT IS NULL
            ORDER BY t1.MODIFY_TIME DESC
            ";

        return await QueryListAsync<ProductionLotEntity>(
            sql,
            new { WorkFlow = workFlow, LotIdOrSn = lotIdOrSn },
            cancellationToken);
    }

    public async Task<ProductionLotEntity?> GetLotMasterAsync(string lotId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                PRODUCTION_LOT_NO AS LotId,
                JOB_NUM AS WorkOrder,
                PART_NO AS ProductPartNumber,
                IS_FINISHED AS IsFinished,
                FORM_NO AS FormNo,
                PROJ_CODE AS ProjectCode,
                PRODUCT AS WorkFlow,
                PROCESS_QTY AS ProcessQuantity,
                MARKING AS LotMarking
            FROM PRODUCTION_LOT_MASTER
            WHERE PRODUCTION_LOT_NO = :LotId
            ";

        return await QueryFirstOrDefaultAsync<ProductionLotEntity>(
            sql,
            new { LotId = lotId },
            cancellationToken);
    }

    public async Task<ProductionRecipeEntity?> GetRecipeAsync(string projectCode, string workFlow, string process, string formNo, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT RECIPE_NAME AS Recipe, STATUS AS Status
            FROM PROJECT_PROCESS_RECIPE_SETTING
            WHERE PROJ_CODE = :ProjectCode
              AND WORK_FLOW = :WorkFlow
              AND PROCESS_NAME = :Process
              AND FORM_NO = :FormNo
            ";

        var result = await QueryFirstOrDefaultAsync<ProductionRecipeEntity>(
            sql,
            new { ProjectCode = projectCode, WorkFlow = workFlow, Process = process, FormNo = formNo },
            cancellationToken);

        if (result is not null)
        {
            return result;
        }

        string commonSql = @"
            SELECT RECIPE_NAME AS Recipe, STATUS AS Status
            FROM PROJECT_PROCESS_RECIPE_SETTING
            WHERE PROJ_CODE = :ProjectCode
              AND WORK_FLOW = :WorkFlow
              AND PROCESS_NAME = :Process
              AND FORM_NO LIKE '%COMM%'
            ";

        var commonResult = await QueryFirstOrDefaultAsync<ProductionRecipeEntity>(
            commonSql,
            new { ProjectCode = projectCode, WorkFlow = workFlow, Process = process },
            cancellationToken);

        return commonResult;
    }

    public Task<IReadOnlyList<string>> GetToolRequirementsAsync(string productPartNumber, string workFlow, string process, string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT t1.TOOL_PN
            FROM PRODUCT_PROCESS_TOOL_SETTING t1
            INNER JOIN EQUIPMENT_LIST t2 ON t1.EQUIPMENT_FAMILY = t2.EQUIPMENT_FAMILY
            WHERE t1.PRODUCT_PN = :ProductPartNumber
              AND t1.WORK_FLOW = :WorkFlow
              AND t1.PROCESS = :Process
              AND t2.EQUIPMENT_ID = :EquipmentId
            ";

        return QueryListAsync<string>(
            sql,
            new { ProductPartNumber = productPartNumber, WorkFlow = workFlow, Process = process, EquipmentId = equipmentId },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionMaterialRequirementEntity>> GetMaterialRequirementsAsync(string productPartNumber, string workFlow, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                t1.COMPONENT_NAME AS ComponentName,
                t1.COMPONENT_PN AS ComponentPartNumber,
                t2.COMPONENT_USAGE AS ConsumptionFactor
            FROM PRODUCT_COMPONENT_BOM_LINK_PN t1
            INNER JOIN PRODUCT_COMPONENT_BOM t2
                ON t1.PRODUCT = t2.PRODUCT
                AND t1.COMPONENT_NAME = t2.COMPONENT_NAME
            WHERE t1.PRODUCT_PN = :ProductPartNumber
              AND t1.PRODUCT = :WorkFlow
              AND t2.PROCESS_NAME = :Process
            ";

        return await QueryListAsync<ProductionMaterialRequirementEntity>(
            sql,
            new { ProductPartNumber = productPartNumber, WorkFlow = workFlow, Process = process },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessDefectEntity>> GetProcessDefectsAsync(string workFlow, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                t1.PRODUCT AS WorkFlow,
                t1.CATEGORY AS Process,
                t1.CAT AS DefectCode,
                t2.DEFECT_NAME AS DefectName,
                t1.FIELD_NAME AS FieldName,
                t1.TABLE_NAME AS TableName
            FROM DATA_DICTIONARY t1
            INNER JOIN DEFECT_ALL t2 ON t1.CAT = t2.DEFECT_CODE
            WHERE t1.PRODUCT = :WorkFlow
              AND t1.CATEGORY = :Process
            ORDER BY t1.CAT
            ";

        return await QueryListAsync<ProcessDefectEntity>(
            sql,
            new { WorkFlow = workFlow, Process = process },
            cancellationToken);
    }

    private async Task<ProductionEquipmentOperationEntity?> MapOperationAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        return await QueryFirstOrDefaultAsync<ProductionEquipmentOperationEntity>(sql, parameters, cancellationToken);
    }
}
