using System.Data.Common;
using Dapper;
using EAP.Application.Interfaces.Commands;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Requests.Transactions;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class HoldLotRepository : RepositoryBase, IHoldLotRepository
{
    private const string DatabaseName = "DgFisOracle";

    public HoldLotRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<int> HoldAsync(HoldLotRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(
            (connection, transaction, token) => InsertHoldAsync(
                connection,
                transaction,
                request,
                token),
            cancellationToken);
    }

    public async Task<(string? WorkFlow, string? Process, string? OperatorId)?> GetEquipmentProcessAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT WORK_FLOW AS WorkFlow, PROCESS AS Process, EMPLOYEE_ID AS OperatorId
            FROM EQUIPMENT_PROCESS_EMPLOYEE_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
            ";

        var row = await QueryFirstOrDefaultAsync<HoldWipRow>(
            sql,
            new { EquipmentId = equipmentId },
            cancellationToken);

        return row is null ? null : (row.WorkFlow, row.Process, row.OperatorId);
    }

    public Task<decimal> GetNextHoldNumberAsync(CancellationToken cancellationToken)
    {
        string sql = "SELECT SEQ_HOLD_NUMBER.NEXTVAL FROM DUAL";
        return QuerySingleAsync<decimal>(sql, parameters: null, cancellationToken);
    }

    private async Task<int> InsertHoldAsync(DbConnection connection, DbTransaction transaction, HoldLotRequest request, CancellationToken cancellationToken)
    {
        string headSql = @"
            INSERT INTO HOLD_HEAD
                (HOLD_NUMBER, PRODUCT, PROJECT, OA_APPROVE_NO, BIND,
                 CREATE_TIME, CREATE_USER, UPDATE_TIME, UPDATE_USER)
            VALUES
                (:HoldNumber, :Product, NVL(FUN_GET_PROJECT_BY_LOT(:LotId),'0'), 'NA', '1',
                 SYSDATE, :CreateUser, SYSDATE, :UpdateUser)
            ";

        var headCommand = new CommandDefinition(
            headSql,
            new
            {
                HoldNumber = request.HoldNumber,
                Product = request.WorkFlow,
                LotId = request.Items[0].LotId,
                CreateUser = request.OperatorId,
                UpdateUser = request.OperatorId
            },
            transaction,
            CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(headCommand);

        string detailSql = @"
            INSERT INTO HOLD_DETAIL
                (HOLD_NUMBER, SEQ, PRODUCT, PROJECT, PROCESS, BIND,
                 BARCODE_TYPE, HOLD_TYPE, SCAN_BARCODE, WO_LOTNO, SN,
                 HOLD_REASON, CREATE_TIME, CREATE_USER, UPDATE_TIME, UPDATE_USER)
            VALUES
                (:HoldNumber, SEQ_HOLD_DETAIL.NEXTVAL, :Product,
                 NVL(FUN_GET_PROJECT_BY_LOT(:LotId),'0'), :Process, '1', :BarcodeType,
                 'RealTime', :Barcode, :LotId, :Sn, :HoldReason,
                 SYSDATE, :CreateUser, SYSDATE, :UpdateUser)
            ";

        foreach (var item in request.Items)
        {
            var barcodeType = string.IsNullOrWhiteSpace(item.Sn) ? "LOT" : "SN";
            var detailCommand = new CommandDefinition(
                detailSql,
                new
                {
                    HoldNumber = request.HoldNumber,
                    Product = request.WorkFlow,
                    Process = request.Process,
                    LotId = item.LotId,
                    Sn = item.Sn,
                    Barcode = string.IsNullOrWhiteSpace(item.Sn) ? item.LotId : item.Sn,
                    request.HoldReason,
                    CreateUser = request.OperatorId,
                    UpdateUser = request.OperatorId,
                    BarcodeType = barcodeType
                },
                transaction,
                CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            affected += await connection.ExecuteAsync(detailCommand);
        }

        return affected;
    }

    private sealed class HoldWipRow
    {
        public string? WorkFlow { get; set; }

        public string? Process { get; set; }

        public string? OperatorId { get; set; }
    }
}
