using Dapper;
using System.Text.RegularExpressions;
using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Tracking;
using EAP.Models.Requests.Production;

namespace EAP.DataAccess.Repositories;

public sealed class ProductionTrackingRepository : RepositoryBase, IProductionTrackingRepository
{
    private const string DatabaseName = "DgFisOracle";

    public ProductionTrackingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<bool> IsTransactionCompletedAsync(string transactionId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM PRODUCTION_TRANSACTION
            WHERE TRANSACTION_ID = :TransactionId
              AND STATUS = 1
            ";
        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { TransactionId = transactionId },
            cancellationToken);
        return count > 0;
    }

    public Task<ProductionTrackingContextEntity?> GetTrackInContextAsync(TrackInRequest request, CancellationToken cancellationToken)
    {
        return GetContextAsync(
            request.EquipmentId,
            request.LotId ?? string.Empty,
            request.WorkFlow,
            request.Process,
            request.EmployeeId,
            cancellationToken);
    }

    public Task<ProductionTrackingContextEntity?> GetTrackOutContextAsync(TrackOutRequest request, CancellationToken cancellationToken)
    {
        return GetContextAsync(
            request.EquipmentId,
            request.LotId,
            request.WorkFlow,
            request.Process,
            request.EmployeeId,
            cancellationToken);
    }

    public Task<ProductionLotWipEntity?> GetLotWipAsync(string lotId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                LOT_PROCESS_SEQ AS LotProcessSequence,
                LOT_ID AS LotId,
                WORK_FLOW AS WorkFlow,
                TRACE_WAY AS TraceWay,
                PROCESS AS Process,
                PROJ_CODE AS ProjectCode,
                WORK_ORDER AS WorkOrder,
                PRODUCT_PN AS ProductPartNumber,
                TRACK_IN_TIME AS TrackInTime,
                TRACK_IN_QTY AS TrackInQuantity,
                STATUS AS Status,
                OPERATOR_ID AS OperatorId,
                TESTER AS Tester,
                RECIPE_NAME AS RecipeName,
                CARRIER_ID AS CarrierId,
                ALLOW_ONLINE_REWORK AS AllowOnlineRework,
                FORM_NO AS FormNo
            FROM PRODUCTION_LOT_WIP
            WHERE LOT_ID = :LotId
            ";
        return QueryFirstOrDefaultAsync<ProductionLotWipEntity>(
            sql,
            new { LotId = lotId },
            cancellationToken);
    }

    public Task<IReadOnlyList<ProductionSnWipEntity>> GetSnWipsAsync(string lotId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                SN_PROCESS_SEQ AS SnProcessSequence,
                LOT_PROCESS_SEQ AS LotProcessSequence,
                LOT_ID AS LotId,
                SN AS SerialNumber,
                WORK_FLOW AS WorkFlow,
                TRACE_WAY AS TraceWay,
                PROCESS AS Process,
                LOADING_TIME AS LoadingTime,
                TRACK_IN_TIME AS TrackInTime,
                STATUS AS Status,
                TESTER AS Tester,
                PROJ_CODE AS ProjectCode,
                WORK_ORDER AS WorkOrder,
                PRODUCT_PN AS ProductPartNumber,
                PROCESS_RESULT AS ProcessResult,
                DEFECT_CODE AS DefectCode,
                DEFECT_NAME AS DefectName,
                TEST_DEFECT AS TestDefect,
                ONLINE_REWORK AS OnlineRework,
                OPERATOR_ID AS OperatorId,
                RECIPE_NAME AS RecipeName,
                CARRIER_ID AS CarrierId
            FROM PRODUCTION_LOT_SN_WIP
            WHERE LOT_ID = :LotId
            ";
        return QueryListAsync<ProductionSnWipEntity>(
            sql,
            new { LotId = lotId },
            cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetDefectCodesAsync(string workFlow, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT t1.CAT
            FROM DATA_DICTIONARY t1
            INNER JOIN DEFECT_ALL t2 ON t1.CAT = t2.DEFECT_CODE
            WHERE t1.PRODUCT = :WorkFlow
              AND t1.CATEGORY = :Process
            ";
        var codes = await QueryListAsync<string>(
            sql,
            new { WorkFlow = workFlow, Process = process },
            cancellationToken);
        return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ProductionMaterialEntity>> GetMaterialsAsync(string productPartNumber, string workFlow, string process, string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                :EquipmentId AS EquipmentId,
                link.COMPONENT_NAME AS ComponentName,
                link.COMPONENT_PN AS ComponentPartNumber,
                bom.COMPONENT_USAGE AS ConsumptionFactor,
                material.MATERIAL_LOT AS MaterialLot,
                material.MATERIAL_PN AS MaterialPartNumber,
                NVL(material.ORIGINAL_QTY, 0) AS OriginalQuantity,
                NVL(material.REMAIN_QTY, 0) AS RemainingQuantity,
                material.EXPIRE_TIME AS ExpireTime,
                COUNT(DISTINCT link.COMPONENT_NAME) OVER () AS RequiredComponentCount,
                (SELECT COUNT(1)
                 FROM EQUIPMENT_MATERIAL_WIP loaded
                 WHERE loaded.EQUIPMENT_ID = :EquipmentId) AS LoadedComponentCount
            FROM PRODUCT_COMPONENT_BOM_LINK_PN link
            INNER JOIN PRODUCT_COMPONENT_BOM bom
                ON link.PRODUCT = bom.PRODUCT
                AND link.COMPONENT_NAME = bom.COMPONENT_NAME
            LEFT JOIN EQUIPMENT_MATERIAL_WIP material
                ON material.EQUIPMENT_ID = :EquipmentId
                AND material.COMPONENT_NAME = link.COMPONENT_NAME
                AND material.MATERIAL_PN = link.COMPONENT_PN
            WHERE link.PRODUCT_PN = :ProductPartNumber
              AND link.PRODUCT = :WorkFlow
              AND bom.PROCESS_NAME = :Process
            ";
        return QueryListAsync<ProductionMaterialEntity>(sql, new
        {
            ProductPartNumber = productPartNumber,
            WorkFlow = workFlow,
            Process = process,
            EquipmentId = equipmentId
        }, cancellationToken);
    }

    public Task<int> ExecuteTrackInAsync(TrackInRequest request, ProductionTrackingContextEntity context, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                string existingLotSql = @"
                    SELECT LOT_PROCESS_SEQ AS LotProcessSequence,
                           WORK_FLOW AS WorkFlow,
                           PROCESS AS Process
                    FROM PRODUCTION_LOT_WIP
                    WHERE LOT_ID = :LotId
                    ";
                var existingLot = await connection.QueryFirstOrDefaultAsync<ExistingLotRow>(
                    CreateCommand(existingLotSql, new { context.LotId }, token, transaction));
                if (existingLot is not null)
                {
                    if (context.TraceWay != 2
                        || string.IsNullOrWhiteSpace(request.SerialNumber)
                        || !string.Equals(existingLot.WorkFlow, context.WorkFlow, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(existingLot.Process, context.Process, StringComparison.OrdinalIgnoreCase))
                    {
                        return 0;
                    }

                    string updateSnSql = @"
                        UPDATE PRODUCTION_LOT_SN_WIP
                        SET STATUS = 1,
                            TRACK_IN_TIME = :TransactionTime,
                            TESTER = :EquipmentId,
                            OPERATOR_ID = :OperatorId,
                            RECIPE_NAME = :RecipeName,
                            ONLINE_REWORK = 0,
                            UPDATE_TIME = SYSDATE
                        WHERE LOT_ID = :LotId
                          AND SN = :SerialNumber
                          AND STATUS = 0
                        ";
                    var updated = await connection.ExecuteAsync(CreateCommand(updateSnSql, new
                    {
                        request.TransactionTime,
                        request.EquipmentId,
                        context.OperatorId,
                        request.RecipeName,
                        context.LotId,
                        request.SerialNumber
                    }, token, transaction));
                    if (updated > 0)
                    {
                        await SaveTransactionAsync(connection, transaction, request.TransactionId, request.TransactionTime, "TrackIn", context.OperatorId, token);
                    }

                    return updated;
                }

                var lotSequence = await connection.QuerySingleAsync<decimal>(
                    CreateCommand("SELECT LOT_PROCESS_SEQ.NEXTVAL FROM DUAL", null, token, transaction));
                var serialNumbers = context.TraceWay == 2
                    ? (await connection.QueryAsync<string>(CreateCommand(
                        "SELECT KEY_MATERIAL_SN FROM SN_MASTER WHERE PRODUCTION_LOT_NO = :LotId AND KEY_MATERIAL_SN IS NOT NULL",
                        new { context.LotId }, token, transaction))).AsList()
                    : [];
                if (context.TraceWay == 2
                    && !string.IsNullOrWhiteSpace(request.SerialNumber)
                    && !serialNumbers.Contains(request.SerialNumber, StringComparer.OrdinalIgnoreCase))
                {
                    return 0;
                }
                var trackInQuantity = context.TraceWay == 2
                    ? serialNumbers.Count
                    : context.ProcessQuantity;

                string lotSql = @"
                    INSERT INTO PRODUCTION_LOT_WIP
                        (LOT_PROCESS_SEQ, LOT_ID, WORK_FLOW, TRACE_WAY, PROCESS,
                         ALLOW_ONLINE_REWORK, PROJ_CODE, WORK_ORDER, PRODUCT_PN,
                         TRACK_IN_TIME, TRACK_IN_QTY, STATUS, OPERATOR_ID, TESTER,
                         RECIPE_NAME, CARRIER_ID, FORM_NO, CREATE_TIME, UPDATE_TIME)
                    VALUES
                        (:LotProcessSequence, :LotId, :WorkFlow, :TraceWay, :Process,
                         :OnlineRework, :ProjectCode, :WorkOrder, :ProductPartNumber,
                         :TransactionTime, :TrackInQuantity, 1, :OperatorId, :EquipmentId,
                         :RecipeName, :CarrierId, :FormNo, SYSDATE, SYSDATE)
                    ";
                var affected = await connection.ExecuteAsync(CreateCommand(
                    lotSql,
                    new
                    {
                        LotProcessSequence = lotSequence,
                        context.LotId,
                        context.WorkFlow,
                        context.TraceWay,
                        context.Process,
                        context.OnlineRework,
                        context.ProjectCode,
                        context.WorkOrder,
                        context.ProductPartNumber,
                        request.TransactionTime,
                        TrackInQuantity = trackInQuantity,
                        context.OperatorId,
                        request.EquipmentId,
                        request.RecipeName,
                        context.CarrierId,
                        context.FormNo
                    }, token, transaction));

                if (context.TraceWay == 2)
                {
                    string snSql = @"
                        INSERT INTO PRODUCTION_LOT_SN_WIP
                            (SN_PROCESS_SEQ, LOT_PROCESS_SEQ, LOT_ID, SN, WORK_FLOW,
                             TRACE_WAY, PROCESS, LOADING_TIME, TRACK_IN_TIME, PROJ_CODE,
                             WORK_ORDER, PRODUCT_PN, STATUS, ONLINE_REWORK, OPERATOR_ID,
                             TESTER, RECIPE_NAME, CARRIER_ID, CREATE_TIME, UPDATE_TIME)
                        VALUES
                            (SN_PROCESS_SEQ.NEXTVAL, :LotProcessSequence, :LotId, :SerialNumber,
                             :WorkFlow, :TraceWay, :Process, :TransactionTime, :TrackInTime,
                             :ProjectCode, :WorkOrder, :ProductPartNumber, :Status, 0,
                             :OperatorId, :EquipmentId, :RecipeName, :CarrierId, SYSDATE, SYSDATE)
                        ";
                    foreach (var serialNumber in serialNumbers)
                    {
                        var selected = string.IsNullOrWhiteSpace(request.SerialNumber)
                            || string.Equals(serialNumber, request.SerialNumber, StringComparison.OrdinalIgnoreCase);
                        affected += await connection.ExecuteAsync(CreateCommand(
                            snSql,
                            new
                            {
                                LotProcessSequence = lotSequence,
                                context.LotId,
                                SerialNumber = serialNumber,
                                context.WorkFlow,
                                context.TraceWay,
                                context.Process,
                                request.TransactionTime,
                                TrackInTime = selected ? request.TransactionTime : (DateTime?)null,
                                context.ProjectCode,
                                context.WorkOrder,
                                context.ProductPartNumber,
                                Status = selected ? 1 : 0,
                                context.OperatorId,
                                request.EquipmentId,
                                request.RecipeName,
                                context.CarrierId
                            }, token, transaction));
                    }
                }

                await SaveTransactionAsync(connection, transaction, request.TransactionId,
                    request.TransactionTime, "TrackIn", request.EmployeeId, token);
                return affected;
            },
            cancellationToken);
    }

    public Task<int> ExecuteTrackOutAsync(
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip,
        IReadOnlyList<ProductionSnWipEntity> snWips,
        IReadOnlyList<ProductionMaterialEntity> materials,
        CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                var affected = await InsertMaterialHistoryAsync(
                    connection,
                    transaction,
                    token,
                    request,
                    context,
                    lotWip,
                    snWips,
                    materials);
                if (request.TraceWay == 1)
                {
                    string historySql = @"
                        INSERT INTO PRODUCTION_LOT_QUANTITY_HIST
                            (LOT_PROCESS_SEQ, LOT_ID, QUANTITY, WORK_FLOW, TRACE_WAY,
                             PROCESS, TRACK_IN_TIME, TRACK_OUT_TIME, PROJ_CODE, WORK_ORDER,
                             PRODUCT_PN, STATUS, PROCESS_RESULT, DEFECT_CODE, DEFECT_NAME,
                             TESTER, RECIPE_NAME, OPERATOR_ID, CARRIER_ID, CREATE_TIME)
                        VALUES
                            (:LotProcessSequence, :LotId, :Quantity, :WorkFlow, :TraceWay,
                             :Process, :TrackInTime, :TrackOutTime, :ProjectCode, :WorkOrder,
                             :ProductPartNumber, 2, :ProcessResult, :DefectCode, :DefectName,
                             :Tester, :RecipeName, :OperatorId, :CarrierId, SYSDATE)
                        ";
                    foreach (var item in request.TrackOutLotResultList)
                    {
                        affected += await connection.ExecuteAsync(CreateCommand(historySql, new
                        {
                            lotWip.LotProcessSequence,
                            lotWip.LotId,
                            item.Quantity,
                            lotWip.WorkFlow,
                            lotWip.TraceWay,
                            lotWip.Process,
                            lotWip.TrackInTime,
                            TrackOutTime = request.TransactionTime,
                            lotWip.ProjectCode,
                            lotWip.WorkOrder,
                            lotWip.ProductPartNumber,
                            item.ProcessResult,
                            item.DefectCode,
                            item.DefectName,
                            lotWip.Tester,
                            lotWip.RecipeName,
                            OperatorId = context.OperatorId,
                            lotWip.CarrierId
                        }, token, transaction));
                    }
                }
                else
                {
                    string updateSnSql = @"
                        UPDATE PRODUCTION_LOT_SN_WIP
                        SET TRACK_OUT_TIME = :TrackOutTime,
                            STATUS = :Status,
                            PROCESS_RESULT = :ProcessResult,
                            DEFECT_CODE = :DefectCode,
                            DEFECT_NAME = :DefectName,
                            TEST_DEFECT = :TestDefect,
                            ONLINE_REWORK = :OnlineRework,
                            OPERATOR_ID = :OperatorId,
                            TESTER = :EquipmentId,
                            UPDATE_TIME = SYSDATE
                        WHERE SN_PROCESS_SEQ = :SnProcessSequence
                        ";
                    foreach (var item in request.TrackOutSnResultList)
                    {
                        var wip = snWips.First(row => string.Equals(
                            row.SerialNumber, item.SerialNumber, StringComparison.OrdinalIgnoreCase));
                        if (item.OnlineRework == 1)
                        {
                            affected += await InsertOnlineReworkHistoryAsync(connection, transaction, token, request, context, wip, item);
                        }

                        affected += await connection.ExecuteAsync(CreateCommand(updateSnSql, new
                        {
                            TrackOutTime = item.OnlineRework == 1 ? (DateTime?)null : request.TransactionTime,
                            Status = item.OnlineRework == 1 ? 0 : 2,
                            ProcessResult = item.OnlineRework == 1 ? null : item.ProcessResult,
                            DefectCode = item.OnlineRework == 1 ? null : item.DefectCode,
                            DefectName = item.OnlineRework == 1 ? null : item.DefectName,
                            TestDefect = item.OnlineRework == 1 ? null : item.TestDefect,
                            OnlineRework = 0,
                            OperatorId = context.OperatorId,
                            request.EquipmentId,
                            wip.SnProcessSequence
                        }, token, transaction));

                        if (item.OnlineRework == 1)
                        {
                            string renewSequenceSql = @"
                                UPDATE PRODUCTION_LOT_SN_WIP
                                SET SN_PROCESS_SEQ = SN_PROCESS_SEQ.NEXTVAL
                                WHERE LOT_ID = :LotId
                                  AND SN = :SerialNumber
                                ";
                            affected += await connection.ExecuteAsync(CreateCommand(
                                renewSequenceSql,
                                new { request.LotId, item.SerialNumber },
                                token,
                                transaction));
                        }
                    }
                }

                var isCompleted = request.TraceWay == 1;
                if (request.TraceWay == 2)
                {
                    string pendingSql = @"
                        SELECT COUNT(1)
                        FROM PRODUCTION_LOT_SN_WIP
                        WHERE LOT_PROCESS_SEQ = :LotProcessSequence
                          AND STATUS IN (0, 1)
                        ";
                    var pendingCount = await connection.QuerySingleAsync<int>(CreateCommand(
                        pendingSql,
                        new { lotWip.LotProcessSequence },
                        token,
                        transaction));
                    isCompleted = pendingCount == 0;
                }

                if (isCompleted)
                {
                    affected += await InsertDefectSummaryAsync(connection, transaction, token, request, context, lotWip);
                    affected += await InsertProcessDataAsync(connection, transaction, token, request, context, lotWip, materials);
                    affected += await CompleteLotAsync(connection, transaction, token, request, context, lotWip);
                }

                await SaveTransactionAsync(connection, transaction, request.TransactionId, request.TransactionTime, "TrackOut", request.EmployeeId, token);
                return affected;
            },
            cancellationToken);
    }

    private async Task<int> InsertDefectSummaryAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip)
    {
        string configurationSql = @"
            SELECT CAT AS DefectCode, FIELD_NAME AS FieldName
            FROM DATA_DICTIONARY
            WHERE PRODUCT = :WorkFlow
              AND CATEGORY = :Process
            ";
        var configurations = (await connection.QueryAsync<DefectFieldRow>(CreateCommand(
            configurationSql,
            new { context.WorkFlow, context.Process },
            cancellationToken,
            transaction))).AsList();

        var parameters = new DynamicParameters();
        parameters.Add("Product", context.WorkFlow);
        parameters.Add("Process", context.Process);
        parameters.Add("InputTime", lotWip.TrackInTime);
        parameters.Add("LotId", lotWip.LotId);
        parameters.Add("ProjectCode", lotWip.ProjectCode);
        parameters.Add("TotalQuantity", lotWip.TrackInQuantity);
        parameters.Add("Marking", "N");

        var columns = new List<string>
        {
            "PRODUCT", "PROCESS_NAME", "INPUT_TIME", "KEY1", "KEY2", "KEY3",
            "INFO1", "INFO2", "INFO3", "INFO4", "TTL_QTY", "FAIL_QTY", "MARKING"
        };
        var values = new List<string>
        {
            ":Product", ":Process", ":InputTime", ":LotId", "'-'", "'-'",
            ":ProjectCode", "'-'", "'-'", "'-'", ":TotalQuantity", ":FailQuantity", ":Marking"
        };

        if (request.TraceWay == 1)
        {
            var failures = request.TrackOutLotResultList
                .Where(item => string.Equals(item.ProcessResult, "FAIL", StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.DefectCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key ?? string.Empty, group => group.Sum(item => item.Quantity), StringComparer.OrdinalIgnoreCase);
            parameters.Add("FailQuantity", failures.Values.Sum());
            AddDefectFields(configurations, failures, columns, values, parameters);
            return await InsertDynamicRowAsync(connection, transaction, cancellationToken, "DEFECT_PROD", columns, values, parameters);
        }

        string failedSnSql = @"
            SELECT SN AS SerialNumber, DEFECT_CODE AS DefectCode
            FROM PRODUCTION_LOT_SN_WIP
            WHERE LOT_PROCESS_SEQ = :LotProcessSequence
              AND UPPER(PROCESS_RESULT) = 'FAIL'
            ";
        var failedSerials = (await connection.QueryAsync<FailedSerialRow>(CreateCommand(
            failedSnSql,
            new { lotWip.LotProcessSequence },
            cancellationToken,
            transaction))).AsList();
        var serialsByDefect = failedSerials
            .GroupBy(item => item.DefectCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key ?? string.Empty,
                group => string.Join(Environment.NewLine, group.Select(item => item.SerialNumber)),
                StringComparer.OrdinalIgnoreCase);
        parameters.Add("FailQuantity", failedSerials.Count);
        AddDefectFields(configurations, serialsByDefect, columns, values, parameters);
        return await InsertDynamicRowAsync(connection, transaction, cancellationToken, "DEFECT_SN_PROD", columns, values, parameters);
    }

    private async Task<int> InsertProcessDataAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip,
        IReadOnlyList<ProductionMaterialEntity> materials)
    {
        if (string.IsNullOrWhiteSpace(context.ProcessTable))
        {
            return 0;
        }

        string configurationSql = @"
            SELECT
                item.FIELD_NAME AS FieldName,
                item.FIELD_TYPE AS FieldType,
                item.DEFAULT_VALUE AS DefaultValue,
                field.FIELD_CATEGORY1 AS FieldCategory1,
                field.FIELD_CATEGORY2 AS FieldCategory2,
                field.FIELD_CATEGORY3 AS FieldCategory3
            FROM PROCESS_ITEM_INFO item
            LEFT JOIN PROCESS_DATA_COLLECT_FIELD field
                ON item.FIELD_NAME = field.FIELD_NAME
            WHERE item.PRODUCT = :WorkFlow
              AND item.PROCESS_NAME = :Process
              AND item.OPERATE_STYLE = 'N'
              AND item.FIELD_NAME NOT IN ('TEMPERATURE', 'COUNTTIME')
            ORDER BY item.FIELD_SEQ
            ";
        var items = (await connection.QueryAsync<ProcessFieldRow>(CreateCommand(
            configurationSql,
            new { context.WorkFlow, context.Process },
            cancellationToken,
            transaction))).AsList();
        if (items.Count == 0)
        {
            return 0;
        }

        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new DynamicParameters();
        foreach (var item in items)
        {
            EnsureIdentifier(item.FieldName);
            columns.Add(item.FieldName);
            values.Add($":{item.FieldName}");
            parameters.Add(item.FieldName, ResolveProcessFieldValue(item, request, context, lotWip, materials));
        }

        return await InsertDynamicRowAsync(connection, transaction, cancellationToken, context.ProcessTable, columns, values, parameters);
    }

    private async Task<int> InsertMaterialHistoryAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip,
        IReadOnlyList<ProductionSnWipEntity> snWips,
        IReadOnlyList<ProductionMaterialEntity> materials)
    {
        var affected = 0;
        foreach (var material in materials.Where(item => !string.IsNullOrWhiteSpace(item.MaterialLot)))
        {
            if (request.TraceWay == 1)
            {
                string lotSql = @"
                    INSERT INTO PRODUCTION_COMPONENT_LOT_HIST
                        (LOT_PROCESS_SEQ, LOT_ID, EQUIPMENT_ID, WORK_FLOW, PROCESS_NAME,
                         COMPONENT_NAME, MATERIAL_LOT, MATERIAL_PN, CONSUME_FACTOR,
                         USAGE, EVENT_TIME)
                    VALUES
                        (:LotProcessSequence, :LotId, :EquipmentId, :WorkFlow, :Process,
                         :ComponentName, :MaterialLot, :MaterialPartNumber,
                         :ConsumptionFactor, :Usage, :EventTime)
                    ";
                affected += await connection.ExecuteAsync(CreateCommand(lotSql, new
                {
                    lotWip.LotProcessSequence,
                    lotWip.LotId,
                    request.EquipmentId,
                    context.WorkFlow,
                    context.Process,
                    material.ComponentName,
                    material.MaterialLot,
                    material.MaterialPartNumber,
                    material.ConsumptionFactor,
                    Usage = request.TrackOutLotResultList.Sum(item => item.Quantity) * material.ConsumptionFactor,
                    EventTime = request.TransactionTime
                }, cancellationToken, transaction));
                continue;
            }

            string snSql = @"
                INSERT INTO PRODUCTION_COMPONENT_SN_HIST
                    (SN_PROCESS_SEQ, LOT_ID, SN, EQUIPMENT_ID, WORK_FLOW, PROCESS_NAME,
                     COMPONENT_NAME, MATERIAL_LOT, MATERIAL_PN, CONSUME_FACTOR,
                     USAGE, EVENT_TIME)
                VALUES
                    (:SnProcessSequence, :LotId, :SerialNumber, :EquipmentId,
                     :WorkFlow, :Process, :ComponentName, :MaterialLot,
                     :MaterialPartNumber, :ConsumptionFactor, :ConsumptionFactor,
                     :EventTime)
                ";
            foreach (var result in request.TrackOutSnResultList)
            {
                var snWip = snWips.First(row => string.Equals(
                    row.SerialNumber,
                    result.SerialNumber,
                    StringComparison.OrdinalIgnoreCase));
                affected += await connection.ExecuteAsync(CreateCommand(snSql, new
                {
                    snWip.SnProcessSequence,
                    request.LotId,
                    result.SerialNumber,
                    request.EquipmentId,
                    context.WorkFlow,
                    context.Process,
                    material.ComponentName,
                    material.MaterialLot,
                    material.MaterialPartNumber,
                    material.ConsumptionFactor,
                    EventTime = request.TransactionTime
                }, cancellationToken, transaction));
            }
        }

        return affected;
    }

    private async Task<int> InsertOnlineReworkHistoryAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionSnWipEntity wip,
        TrackOutRequest.SnResult result)
    {
        string sql = @"
            INSERT INTO PRODUCTION_LOT_SN_HIST
                (SN_PROCESS_SEQ, LOT_PROCESS_SEQ, LOT_ID, SN, WORK_FLOW, TRACE_WAY,
                 PROCESS, LOADING_TIME, TRACK_IN_TIME, TRACK_OUT_TIME, PROJ_CODE,
                 WORK_ORDER, PRODUCT_PN, STATUS, PROCESS_RESULT, DEFECT_CODE,
                 DEFECT_NAME, TEST_DEFECT, ONLINE_REWORK, OPERATOR_ID, TESTER,
                 RECIPE_NAME, CARRIER_ID, CREATE_TIME, UPDATE_TIME)
            VALUES
                (:SnProcessSequence, :LotProcessSequence, :LotId, :SerialNumber,
                 :WorkFlow, :TraceWay, :Process, :LoadingTime, :TrackInTime,
                 :TrackOutTime, :ProjectCode, :WorkOrder, :ProductPartNumber, 2,
                 :ProcessResult, :DefectCode, :DefectName, :TestDefect, 1,
                 :OperatorId, :EquipmentId, :RecipeName, :CarrierId, SYSDATE, SYSDATE)
            ";
        return await connection.ExecuteAsync(CreateCommand(sql, new
        {
            wip.SnProcessSequence,
            wip.LotProcessSequence,
            wip.LotId,
            wip.SerialNumber,
            wip.WorkFlow,
            wip.TraceWay,
            wip.Process,
            wip.LoadingTime,
            wip.TrackInTime,
            TrackOutTime = request.TransactionTime,
            wip.ProjectCode,
            wip.WorkOrder,
            wip.ProductPartNumber,
            result.ProcessResult,
            result.DefectCode,
            result.DefectName,
            result.TestDefect,
            OperatorId = context.OperatorId,
            request.EquipmentId,
            wip.RecipeName,
            wip.CarrierId
        }, cancellationToken, transaction));
    }

    private async Task<int> CompleteLotAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip)
    {
        var trackOutQuantity = request.TraceWay == 1
            ? request.TrackOutLotResultList
                .Where(item => string.Equals(item.ProcessResult, "PASS", StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Quantity)
            : await connection.QuerySingleAsync<int>(CreateCommand(
                @"SELECT COUNT(1)
                  FROM PRODUCTION_LOT_SN_WIP
                  WHERE LOT_PROCESS_SEQ = :LotProcessSequence
                    AND STATUS = 2
                    AND UPPER(PROCESS_RESULT) = 'PASS'",
                new { lotWip.LotProcessSequence },
                cancellationToken,
                transaction));

        var affected = 0;
        if (request.TraceWay == 2)
        {
            string updateSnMasterSql = @"
                UPDATE SN_MASTER master
                SET PROCESS = :Process,
                    MODIFY_TIME = SYSDATE,
                    DEFECT =
                    (
                        SELECT defect.DEFECT_NAME
                        FROM PRODUCTION_LOT_SN_WIP wip
                        LEFT JOIN DEFECT_ALL defect
                            ON defect.DEFECT_CODE = wip.DEFECT_CODE
                        WHERE wip.LOT_PROCESS_SEQ = :LotProcessSequence
                          AND wip.SN = master.KEY_MATERIAL_SN
                    ),
                    DEFECT_FIELD =
                    (
                        SELECT dictionary.FIELD_NAME
                        FROM PRODUCTION_LOT_SN_WIP wip
                        LEFT JOIN DATA_DICTIONARY dictionary
                            ON dictionary.PRODUCT = :WorkFlow
                            AND dictionary.CATEGORY = :Process
                            AND dictionary.CAT = wip.DEFECT_CODE
                        WHERE wip.LOT_PROCESS_SEQ = :LotProcessSequence
                          AND wip.SN = master.KEY_MATERIAL_SN
                    )
                WHERE master.PRODUCTION_LOT_NO = :LotId
                  AND EXISTS
                  (
                      SELECT 1
                      FROM PRODUCTION_LOT_SN_WIP wip
                      WHERE wip.LOT_PROCESS_SEQ = :LotProcessSequence
                        AND wip.SN = master.KEY_MATERIAL_SN
                  )
                ";
            affected += await connection.ExecuteAsync(CreateCommand(updateSnMasterSql, new
            {
                context.Process,
                context.WorkFlow,
                lotWip.LotProcessSequence,
                lotWip.LotId
            }, cancellationToken, transaction));

            string snHistorySql = @"
                INSERT INTO PRODUCTION_LOT_SN_HIST
                    (SN_PROCESS_SEQ, LOT_PROCESS_SEQ, LOT_ID, SN, WORK_FLOW, TRACE_WAY,
                     PROCESS, LOADING_TIME, TRACK_IN_TIME, TRACK_OUT_TIME, PROJ_CODE,
                     WORK_ORDER, PRODUCT_PN, STATUS, PROCESS_RESULT, DEFECT_CODE,
                     DEFECT_NAME, TEST_DEFECT, ONLINE_REWORK, OPERATOR_ID, TESTER,
                     RECIPE_NAME, CARRIER_ID, CREATE_TIME, UPDATE_TIME)
                SELECT SN_PROCESS_SEQ, LOT_PROCESS_SEQ, LOT_ID, SN, WORK_FLOW, TRACE_WAY,
                       PROCESS, LOADING_TIME, TRACK_IN_TIME, TRACK_OUT_TIME, PROJ_CODE,
                       WORK_ORDER, PRODUCT_PN, STATUS, PROCESS_RESULT, DEFECT_CODE,
                       DEFECT_NAME, TEST_DEFECT, ONLINE_REWORK, OPERATOR_ID, TESTER,
                       RECIPE_NAME, CARRIER_ID, SYSDATE, SYSDATE
                FROM PRODUCTION_LOT_SN_WIP
                WHERE LOT_PROCESS_SEQ = :LotProcessSequence
                ";
            affected += await connection.ExecuteAsync(CreateCommand(
                snHistorySql,
                new { lotWip.LotProcessSequence },
                cancellationToken,
                transaction));
        }

        string lotHistorySql = @"
            INSERT INTO PRODUCTION_LOT_HIST
                (LOT_PROCESS_SEQ, LOT_ID, WORK_FLOW, TRACE_WAY, PROCESS,
                 ALLOW_ONLINE_REWORK, PROJ_CODE, WORK_ORDER, PRODUCT_PN,
                 TRACK_IN_TIME, TRACK_IN_QTY, TRACK_OUT_TIME, TRACK_OUT_QTY,
                 STATUS, OPERATOR_ID, TESTER, RECIPE_NAME, FORM_NO, CARRIER_ID,
                 CREATE_TIME, UPDATE_TIME)
            VALUES
                (:LotProcessSequence, :LotId, :WorkFlow, :TraceWay, :Process,
                 :AllowOnlineRework, :ProjectCode, :WorkOrder, :ProductPartNumber,
                 :TrackInTime, :TrackInQuantity, :TrackOutTime, :TrackOutQuantity,
                 2, :OperatorId, :EquipmentId, :RecipeName, :FormNo, :CarrierId,
                 SYSDATE, SYSDATE)
            ";
        affected += await connection.ExecuteAsync(CreateCommand(lotHistorySql, new
        {
            lotWip.LotProcessSequence,
            lotWip.LotId,
            lotWip.WorkFlow,
            lotWip.TraceWay,
            lotWip.Process,
            lotWip.AllowOnlineRework,
            lotWip.ProjectCode,
            lotWip.WorkOrder,
            lotWip.ProductPartNumber,
            lotWip.TrackInTime,
            lotWip.TrackInQuantity,
            TrackOutTime = request.TransactionTime,
            TrackOutQuantity = trackOutQuantity,
            OperatorId = context.OperatorId,
            request.EquipmentId,
            lotWip.RecipeName,
            lotWip.FormNo,
            lotWip.CarrierId
        }, cancellationToken, transaction));

        string deleteSnSql = @"
            DELETE FROM PRODUCTION_LOT_SN_WIP
            WHERE LOT_PROCESS_SEQ = :LotProcessSequence
            ";
        affected += await connection.ExecuteAsync(CreateCommand(
            deleteSnSql,
            new { lotWip.LotProcessSequence },
            cancellationToken,
            transaction));

        string deleteLotSql = @"
            DELETE FROM PRODUCTION_LOT_WIP
            WHERE LOT_PROCESS_SEQ = :LotProcessSequence
            ";
        affected += await connection.ExecuteAsync(CreateCommand(
            deleteLotSql,
            new { lotWip.LotProcessSequence },
            cancellationToken,
            transaction));

        return affected;
    }

    private Task<ProductionTrackingContextEntity?> GetContextAsync(
        string equipmentId,
        string lotId,
        string? workFlow,
        string? process,
        string? employeeId,
        CancellationToken cancellationToken)
    {
        var explicitProcess = !string.IsNullOrWhiteSpace(workFlow)
            && !string.IsNullOrWhiteSpace(process);
        string sql = explicitProcess
            ? @"
                SELECT
                    :EquipmentId AS EquipmentId,
                    :EmployeeId AS OperatorId,
                    process.PRODUCT AS WorkFlow,
                    process.PROCESS_NAME AS Process,
                    process.PROCESS_DESC AS ProcessTable,
                    product.LOT_CONTROL AS TraceWay,
                    process.ONLINE_REWORK AS OnlineRework,
                    lot.PRODUCTION_LOT_NO AS LotId,
                    lot.PROJ_CODE AS ProjectCode,
                    lot.JOB_NUM AS WorkOrder,
                    lot.PART_NO AS ProductPartNumber,
                    lot.FORM_NO AS FormNo,
                    lot.PROCESS_QTY AS ProcessQuantity,
                    carrier.CARRIER_ID AS CarrierId
                FROM PROCESS_INFO process
                INNER JOIN PRODUCT_INFO product ON process.PRODUCT = product.PRODUCT_CODE
                INNER JOIN PRODUCTION_LOT_MASTER lot ON lot.PRODUCTION_LOT_NO = :LotId
                LEFT JOIN PRODUCTION_CARRIER_WIP carrier ON carrier.PRODUCTION_LOT_NO = lot.PRODUCTION_LOT_NO
                WHERE process.PRODUCT = :WorkFlow
                  AND process.PROCESS_NAME = :Process
                  AND NOT EXISTS
                  (
                      SELECT 1 FROM EQUIPMENT_STATUS_WIP status
                      WHERE status.EQUIPMENT_ID = :EquipmentId
                        AND status.STATUS NOT IN ('Production', 'PRD', 'Standby', 'SBY')
                  )
                "
            : @"
                SELECT
                    binding.EQUIPMENT_ID AS EquipmentId,
                    binding.EMPLOYEE_ID AS OperatorId,
                    binding.WORK_FLOW AS WorkFlow,
                    binding.PROCESS AS Process,
                    process.PROCESS_DESC AS ProcessTable,
                    product.LOT_CONTROL AS TraceWay,
                    process.ONLINE_REWORK AS OnlineRework,
                    lot.PRODUCTION_LOT_NO AS LotId,
                    lot.PROJ_CODE AS ProjectCode,
                    lot.JOB_NUM AS WorkOrder,
                    lot.PART_NO AS ProductPartNumber,
                    lot.FORM_NO AS FormNo,
                    lot.PROCESS_QTY AS ProcessQuantity,
                    carrier.CARRIER_ID AS CarrierId
                FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP binding
                INNER JOIN PROCESS_INFO process
                    ON binding.WORK_FLOW = process.PRODUCT
                    AND binding.PROCESS = process.PROCESS_NAME
                INNER JOIN PRODUCT_INFO product ON binding.WORK_FLOW = product.PRODUCT_CODE
                INNER JOIN PRODUCTION_LOT_MASTER lot ON lot.PRODUCTION_LOT_NO = :LotId
                LEFT JOIN PRODUCTION_CARRIER_WIP carrier ON carrier.PRODUCTION_LOT_NO = lot.PRODUCTION_LOT_NO
                WHERE binding.EQUIPMENT_ID = :EquipmentId
                  AND NOT EXISTS
                  (
                      SELECT 1 FROM EQUIPMENT_STATUS_WIP status
                      WHERE status.EQUIPMENT_ID = :EquipmentId
                        AND status.STATUS NOT IN ('Production', 'PRD', 'Standby', 'SBY')
                  )
                ";
        return QueryFirstOrDefaultAsync<ProductionTrackingContextEntity>(sql, new
        {
            EquipmentId = equipmentId,
            LotId = lotId,
            WorkFlow = workFlow,
            Process = process,
            EmployeeId = employeeId
        }, cancellationToken);
    }

    private async Task SaveTransactionAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string transactionId,
        DateTime transactionTime,
        string apiName,
        string? operatorId,
        CancellationToken cancellationToken)
    {
        string deleteSql = @"
            DELETE FROM PRODUCTION_TRANSACTION
            WHERE TRANSACTION_ID = :TransactionId
            ";
        await connection.ExecuteAsync(CreateCommand(
            deleteSql,
            new { TransactionId = transactionId },
            cancellationToken,
            transaction));

        string insertSql = @"
            INSERT INTO PRODUCTION_TRANSACTION
                (TRANSACTION_ID, TRANSACTION_DATE, API_NAME, REQUEST_METHOD, STATUS,
                 MESSAGE, START_TIME, END_TIME, DURATION_MS, CREATE_TIME, CREATE_USER,
                 UPDATE_USER, UPDATE_TIME)
            VALUES
                (:TransactionId, :TransactionTime, :ApiName, 'POST', 1,
                 'Success', SYSDATE, SYSDATE, 0, SYSDATE, :OperatorId,
                 :OperatorId, SYSDATE)
            ";
        await connection.ExecuteAsync(CreateCommand(insertSql, new
        {
            TransactionId = transactionId,
            TransactionTime = transactionTime,
            ApiName = apiName,
            OperatorId = operatorId
        }, cancellationToken, transaction));
    }

    private async Task<int> InsertDynamicRowAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken,
        string tableName,
        IReadOnlyList<string> columns,
        IReadOnlyList<string> values,
        DynamicParameters parameters)
    {
        EnsureQualifiedIdentifier(tableName);
        foreach (var column in columns)
        {
            EnsureIdentifier(column);
        }

        string sql = $@"
            INSERT INTO {tableName} ({string.Join(", ", columns)})
            VALUES ({string.Join(", ", values)})
            ";
        return await connection.ExecuteAsync(CreateCommand(sql, parameters, cancellationToken, transaction));
    }

    private static void AddDefectFields<TValue>(
        IReadOnlyList<DefectFieldRow> configurations,
        IReadOnlyDictionary<string, TValue> failures,
        ICollection<string> columns,
        ICollection<string> values,
        DynamicParameters parameters)
    {
        var parameterIndex = 0;
        foreach (var configuration in configurations)
        {
            if (string.IsNullOrWhiteSpace(configuration.FieldName) || !failures.TryGetValue(configuration.DefectCode, out var value))
            {
                continue;
            }

            EnsureIdentifier(configuration.FieldName);
            var parameterName = $"DefectValue{parameterIndex++}";
            columns.Add(configuration.FieldName);
            values.Add($":{parameterName}");
            parameters.Add(parameterName, value);
        }
    }

    private static object? ResolveProcessFieldValue(
        ProcessFieldRow field,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip,
        IReadOnlyList<ProductionMaterialEntity> materials)
    {
        if (string.Equals(field.FieldCategory1, "FIXED", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertConfiguredValue(field.DefaultValue, field.FieldType);
        }

        if (string.Equals(field.FieldCategory1, "COMPONENT", StringComparison.OrdinalIgnoreCase))
        {
            var material = materials.FirstOrDefault(item => string.Equals(
                item.ComponentName,
                field.FieldCategory2,
                StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.MaterialLot));
            if (material is null)
            {
                throw new InvalidOperationException(
                    $"Process field '{field.FieldName}' requires unloaded component '{field.FieldCategory2}'.");
            }

            return GetComponentFieldValue(field.FieldCategory3, material);
        }

        if (!string.Equals(field.FieldCategory1, "HEAD", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Process field category '{field.FieldCategory1}' is not supported.");
        }

        return GetHeaderFieldValue(field.FieldCategory3, request, context, lotWip);
    }

    private static object? GetComponentFieldValue(string? configuredFieldName, ProductionMaterialEntity material)
    {
        var fieldName = NormalizeFieldName(configuredFieldName);

        if (fieldName == "COMPONENTNAME")
        {
            return material.ComponentName;
        }

        if (fieldName == "EQUIPMENTID")
        {
            return material.EquipmentId;
        }

        if (fieldName == "MATERIALLOT")
        {
            return material.MaterialLot;
        }

        if (fieldName == "MATERIALDATECODE")
        {
            return material.MaterialDateCode;
        }

        if (fieldName == "MATERIALPN" || fieldName == "MATERIALPARTNUMBER")
        {
            return material.MaterialPartNumber;
        }

        if (fieldName == "REMAINQTY" || fieldName == "REMAININGQUANTITY")
        {
            return material.RemainingQuantity;
        }

        if (fieldName == "ORIGINALQTY" || fieldName == "ORIGINALQUANTITY")
        {
            return material.OriginalQuantity;
        }

        if (fieldName == "EXPIRETIME")
        {
            return material.ExpireTime;
        }

        throw new InvalidOperationException(
            $"Process component field mapping '{configuredFieldName}' is not supported.");
    }

    private static object? GetHeaderFieldValue(
        string? configuredFieldName,
        TrackOutRequest request,
        ProductionTrackingContextEntity context,
        ProductionLotWipEntity lotWip)
    {
        var fieldName = NormalizeFieldName(configuredFieldName);

        if (fieldName == "LOTPROCESSSEQ" || fieldName == "LOTPROCESSSEQUENCE")
        {
            return lotWip.LotProcessSequence;
        }

        if (fieldName == "LOTID")
        {
            return lotWip.LotId;
        }

        if (fieldName == "WORKFLOW")
        {
            return context.WorkFlow;
        }

        if (fieldName == "TRACEWAY")
        {
            return request.TraceWay;
        }

        if (fieldName == "PROCESS")
        {
            return context.Process;
        }

        if (fieldName == "ALLOWONLINEREWORK" || fieldName == "ONLINEREWORK")
        {
            return context.OnlineRework;
        }

        if (fieldName == "PROJCODE" || fieldName == "PROJECTCODE")
        {
            return lotWip.ProjectCode;
        }

        if (fieldName == "WORKORDER")
        {
            return lotWip.WorkOrder;
        }

        if (fieldName == "PRODUCTPN" || fieldName == "PRODUCTPARTNUMBER")
        {
            return lotWip.ProductPartNumber;
        }

        if (fieldName == "TRACKINTIME")
        {
            return lotWip.TrackInTime;
        }

        if (fieldName == "TRACKINQTY" || fieldName == "TRACKINQUANTITY")
        {
            return lotWip.TrackInQuantity;
        }

        if (fieldName == "TRACKOUTTIME")
        {
            return request.TransactionTime;
        }

        if (fieldName == "STATUS")
        {
            return 2;
        }

        if (fieldName == "OPERATORID" || fieldName == "EMPLOYEEID")
        {
            return context.OperatorId;
        }

        if (fieldName == "TESTER" || fieldName == "EQUIPMENTID")
        {
            return request.EquipmentId;
        }

        if (fieldName == "RECIPENAME")
        {
            return lotWip.RecipeName;
        }

        if (fieldName == "FORMNO")
        {
            return lotWip.FormNo;
        }

        if (fieldName == "CARRIERID")
        {
            return lotWip.CarrierId;
        }

        if (fieldName == "REMARK")
        {
            return lotWip.Remark;
        }

        if (fieldName == "PROCESSMARKING")
        {
            return lotWip.ProcessMarking;
        }

        if (fieldName == "TRACKOUTQTY" || fieldName == "TRACKOUTQUANTITY")
        {
            if (request.TraceWay == 1)
            {
                return request.TrackOutLotResultList.Sum(item => item.Quantity);
            }

            return request.TrackOutSnResultList.Count;
        }

        throw new InvalidOperationException(
            $"Process header field mapping '{configuredFieldName}' is not supported.");
    }

    private static object? ConvertConfiguredValue(string? value, string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(fieldType, "INT", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var integerValue))
        {
            return integerValue;
        }

        if (string.Equals(fieldType, "DATE", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(value, out var dateValue))
        {
            return dateValue;
        }

        return value;
    }

    private static string NormalizeFieldName(string? value)
    {
        return Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9]", string.Empty).ToUpperInvariant();
    }

    private static void EnsureQualifiedIdentifier(string identifier)
    {
        foreach (var part in identifier.Split('.'))
        {
            EnsureIdentifier(part);
        }
    }

    private static void EnsureIdentifier(string identifier)
    {
        if (!Regex.IsMatch(identifier, "^[A-Za-z_][A-Za-z0-9_$#]*$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException($"Unsafe database identifier '{identifier}'.");
        }
    }

    private sealed class DefectFieldRow
    {
        public string DefectCode { get; set; } = string.Empty;
        public string? FieldName { get; set; }
    }

    private sealed class FailedSerialRow
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string? DefectCode { get; set; }
    }

    private sealed class ProcessFieldRow
    {
        public string FieldName { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public string? DefaultValue { get; set; }
        public string? FieldCategory1 { get; set; }
        public string? FieldCategory2 { get; set; }
        public string? FieldCategory3 { get; set; }
    }

    private sealed class ExistingLotRow
    {
        public decimal LotProcessSequence { get; set; }
        public string WorkFlow { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
    }
}
