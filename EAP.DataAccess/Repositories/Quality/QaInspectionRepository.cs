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
        string sql = @"CALL DB_QA.SP_QA_INSPECTION_PROCESSCODE(@EquipmentId,@ProcessCode);";
        return QueryListAsync<FaiInspectionEntity>(
            sql,
            new { EquipmentId = equipmentId, ProcessCode = processCode },
            cancellationToken);
    }
}
