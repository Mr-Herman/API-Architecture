using System.Text;
using Dapper;
using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.ProcessAssemblies;
using EAP.Models.Requests.ProcessAssemblies;

namespace EAP.DataAccess.Repositories;

public sealed class ProcessAssemblyRepository : RepositoryBase, IProcessAssemblyRepository
{
    private const string DatabaseName = "Dashboard";
    public ProcessAssemblyRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName) { }

    public async Task<(IReadOnlyList<ProcessAssemblyEntity> Items, long TotalCount)> SearchAsync(ProcessAssemblyQueryRequest request, CancellationToken cancellationToken)
    {
        var where = new StringBuilder(" WHERE 1 = 1");
        var parameters = new DynamicParameters();
        AddExactFilter(where, parameters, "JOB_NUM", "JobNum", request.JobNum);
        AddExactFilter(where, parameters, "PART_NO", "PartNo", request.PartNo);
        AddExactFilter(where, parameters, "PROCESS_ID", "ProcessId", request.ProcessId);
        AddExactFilter(where, parameters, "PRODUCTION_LOT_NO", "ProductionLotNo", request.ProductionLotNo);
        AddExactFilter(where, parameters, "OPERATOR_ID", "OperatorId", request.OperatorId);

        if (request.StartTimeFrom.HasValue)
        {
            where.Append(" AND START_TIME >= @StartTimeFrom");
            parameters.Add("StartTimeFrom", request.StartTimeFrom);
        }

        if (request.StartTimeTo.HasValue)
        {
            where.Append(" AND START_TIME <= @StartTimeTo");
            parameters.Add("StartTimeTo", request.StartTimeTo);
        }

        parameters.Add("Offset", ((long)request.PageNumber - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var sql = $@"
            SELECT ID,JOB_NUM,PART_NO,PROJ_CODE,PRODUCT_CODE,PROCESS_ID,PRODUCTION_LOT_NO,MARKING,TESTER,OPERATOR_ID,INPUT_QTY,OUTPUT_QTY,START_TIME,TEST_TIME,REMARK
            FROM dbo.PROCESS_ASSEMBLY_API
            {where}
            ORDER BY ID DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT_BIG(1)
            FROM dbo.PROCESS_ASSEMBLY_API
            {where};
            ";

        using var timing = StartTiming();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);
        var rows = (await results.ReadAsync<ProcessAssemblyRow>()).AsList();
        var items = rows.Select(ToEntity).ToArray();
        var totalCount = await results.ReadSingleAsync<long>();

        return (items, totalCount);
    }

    public async Task<ProcessAssemblyEntity?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken)
    {
        var sql = $@"
            SELECT ID,JOB_NUM,PART_NO,PROJ_CODE,PRODUCT_CODE,PROCESS_ID,PRODUCTION_LOT_NO,MARKING,TESTER,OPERATOR_ID,INPUT_QTY,OUTPUT_QTY,START_TIME,TEST_TIME,REMARK
            FROM dbo.PROCESS_ASSEMBLY_API
            WHERE ID = @Id;

            SELECT
                d.PROJ_CODE,
                d.PRODUCT_CODE,
                d.PROCESS_NAME,
                d.LOT_NO,
                d.SN,
                d.DEFECT_CODE,
                d.DEFECT,
                d.DEFECT_QTY,
                d.START_TIME,
                d.TEST_TIME
            FROM dbo.PROCESS_ASSEMBLY_API AS p
            INNER JOIN dbo.SN_DEFECT_DET_API AS d
                ON d.PROJ_CODE = p.PROJ_CODE
               AND d.PRODUCT_CODE = p.PRODUCT_CODE
               AND d.PROCESS_NAME = p.PROCESS_ID
               AND d.LOT_NO = p.PRODUCTION_LOT_NO
            WHERE p.ID = @Id
            ORDER BY d.SN, d.DEFECT_CODE;
            ";

        using var timing = StartTiming();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);
        var assemblyRow = await results.ReadFirstOrDefaultAsync<ProcessAssemblyRow>();
        if (assemblyRow is null)
        {
            return null;
        }

        var defectRows = (await results.ReadAsync<SnDefectDetailRow>()).AsList();
        var assembly = ToEntity(assemblyRow);
        var defects = defectRows.Select(ToEntity).ToArray();

        assembly.Defects = defects;
        return assembly;
    }

    public async Task<long> CreateAsync(ProcessAssemblyEntity entity, CancellationToken cancellationToken)
    {
        string sql = @$"
            INSERT INTO dbo.PROCESS_ASSEMBLY_API
            (
                JOB_NUM, PART_NO, PROJ_CODE, PRODUCT_CODE, PROCESS_ID,
                PRODUCTION_LOT_NO, MARKING, TESTER, OPERATOR_ID,
                INPUT_QTY, OUTPUT_QTY, START_TIME, TEST_TIME, REMARK
            )
            OUTPUT INSERTED.ID
            VALUES
            (
                @JobNum, @PartNo, @ProjCode, @ProductCode, @ProcessId,
                @ProductionLotNo, @Marking, @Tester, @OperatorId,
                @InputQty, @OutputQty, @StartTime, @TestTime, @Remark
            );
            ";
        return await QuerySingleAsync<long>(sql, entity, cancellationToken);
    }

    public async Task<bool> UpdateAsync(ProcessAssemblyEntity entity, CancellationToken cancellationToken)
    {
        string sql = @"
            UPDATE dbo.PROCESS_ASSEMBLY_API
            SET JOB_NUM = @JobNum,
                PART_NO = @PartNo,
                PROJ_CODE = @ProjCode,
                PRODUCT_CODE = @ProductCode,
                PROCESS_ID = @ProcessId,
                PRODUCTION_LOT_NO = @ProductionLotNo,
                MARKING = @Marking,
                TESTER = @Tester,
                OPERATOR_ID = @OperatorId,
                INPUT_QTY = @InputQty,
                OUTPUT_QTY = @OutputQty,
                START_TIME = @StartTime,
                TEST_TIME = @TestTime,
                REMARK = @Remark
            WHERE ID = @Id;
            ";
        var affectedRows = await ExecuteAsync(sql, entity, cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        string sql = "DELETE FROM dbo.PROCESS_ASSEMBLY_API WHERE ID = @Id;";
        var affectedRows = await ExecuteAsync(sql, new { Id = id }, cancellationToken);

        return affectedRows == 1;
    }

    private static void AddExactFilter(StringBuilder where, DynamicParameters parameters, string column, string parameter, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        where.Append($" AND {column} = @{parameter}");
        parameters.Add(parameter, value);
    }

    private static ProcessAssemblyEntity ToEntity(ProcessAssemblyRow row)
    {
        return new ProcessAssemblyEntity
        {
            Id = row.Id,
            JobNum = row.JobNum,
            PartNo = row.PartNo,
            ProjCode = row.ProjCode,
            ProductCode = row.ProductCode,
            ProcessId = row.ProcessId,
            ProductionLotNo = row.ProductionLotNo,
            Marking = row.Marking,
            Tester = row.Tester,
            OperatorId = row.OperatorId,
            InputQty = row.InputQty,
            OutputQty = row.OutputQty,
            StartTime = row.StartTime,
            TestTime = row.TestTime,
            Remark = row.Remark
        };
    }

    private static SnDefectDetailEntity ToEntity(SnDefectDetailRow row)
    {
        return new SnDefectDetailEntity
        {
            ProjCode = row.ProjCode ?? string.Empty,
            ProductCode = row.ProductCode ?? string.Empty,
            ProcessName = row.ProcessName ?? string.Empty,
            LotNo = row.LotNo ?? string.Empty,
            Sn = row.Sn ?? string.Empty,
            DefectCode = row.DefectCode ?? string.Empty,
            Defect = row.Defect ?? string.Empty,
            DefectQty = row.DefectQty ?? 0,
            StartTime = row.StartTime ?? DateTime.MinValue,
            TestTime = row.TestTime ?? DateTime.MinValue
        };
    }

    private sealed class ProcessAssemblyRow
    {
        public long Id { get; init; }

        public string? JobNum { get; init; }

        public string? PartNo { get; init; }

        public string? ProjCode { get; init; }

        public string? ProductCode { get; init; }

        public string? ProcessId { get; init; }

        public string? ProductionLotNo { get; init; }

        public string? Marking { get; init; }

        public string? Tester { get; init; }

        public string? OperatorId { get; init; }

        public int? InputQty { get; init; }

        public int? OutputQty { get; init; }

        public DateTime? StartTime { get; init; }

        public DateTime? TestTime { get; init; }

        public string? Remark { get; init; }
    }

    private sealed class SnDefectDetailRow
    {
        public string? ProjCode { get; init; }

        public string? ProductCode { get; init; }

        public string? ProcessName { get; init; }

        public string? LotNo { get; init; }

        public string? Sn { get; init; }

        public string? DefectCode { get; init; }

        public string? Defect { get; init; }

        public int? DefectQty { get; init; }

        public DateTime? StartTime { get; init; }

        public DateTime? TestTime { get; init; }
    }
}
