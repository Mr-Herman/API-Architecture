using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Quality;

namespace EAP.DataAccess.Repositories.Quality;

public sealed class QaInspectionRepository : RepositoryBase, IQaInspectionRepository
{
    private const string DatabaseName = "QaMySql";

    public QaInspectionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<string?> GetProcessCodeAsync(string project, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT fis_process_code
            FROM db_qa.eap_fis_process1
            WHERE fis_product = @Project
              AND fis_process_id = @Process
            LIMIT 1
            ";

        return QueryFirstOrDefaultAsync<string>(
            sql,
            new { Project = project, Process = process },
            cancellationToken);
    }

    public async Task<bool> IsEquipmentConfiguredAsync(string processCode, string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM db_qa.eap_fisprocesscode_equipid4
            WHERE fis_process_code = @ProcessCode
              AND equipid = @EquipmentId
            ";

        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { ProcessCode = processCode, EquipmentId = equipmentId },
            cancellationToken);

        return count > 0;
    }

    public Task<IReadOnlyList<FaiInspectionEntity>> GetLatestInspectionsAsync(string equipmentId, string processCode, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                CASE
                    WHEN DATE_ADD(
                        s.save_time,
                        INTERVAL (a.cycle_time + a.delay_time) HOUR
                    ) >= NOW()
                    THEN 'Pass'
                    ELSE 'Fail'
                END AS InspectionResult
            FROM db_qa.eap_equip_cycle_time3 a
            INNER JOIN db_qa.eap_fis_qa2 b
                ON a.qa_id = b.qa_id
            INNER JOIN db_qa.eap_fis_process1 c
                ON b.fis_process_code = c.fis_process_code
            LEFT JOIN
            (
                SELECT
                    scan.tableid,
                    scan.save_time,
                    scan.f6
                FROM db_qa.scan scan
                INNER JOIN
                (
                    SELECT
                        tableid,
                        f6,
                        MAX(save_time) AS max_save_time
                    FROM db_qa.scan
                    GROUP BY tableid, f6
                ) latest
                    ON scan.tableid = latest.tableid
                    AND scan.f6 = latest.f6
                    AND scan.save_time = latest.max_save_time
            ) s
                ON a.qa_id = s.tableid
                AND a.equipid = s.f6
            WHERE a.equipid = @EquipmentId
              AND c.fis_process_code = @ProcessCode
            ORDER BY a.id3
            ";

        return QueryListAsync<FaiInspectionEntity>(
            sql,
            new { EquipmentId = equipmentId, ProcessCode = processCode },
            cancellationToken);
    }
}
