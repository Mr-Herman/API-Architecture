using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Quality;

namespace EAP.DataAccess.Repositories.Quality;

public sealed class SpcInspectionRepository : RepositoryBase, ISpcInspectionRepository
{
    private const string DatabaseName = "SpcOracle";

    public SpcInspectionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<string?> GetProcessCodeAsync(string project, string process, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT fis_process_code
            FROM eap_fis_process1
            WHERE fis_product = :Project
              AND fis_process_id = :Process
            FETCH FIRST 1 ROW ONLY
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
            FROM eap_fisprocesscode_equipid4
            WHERE fis_process_code = :ProcessCode
              AND equipid = :EquipmentId
            ";

        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { ProcessCode = processCode, EquipmentId = equipmentId },
            cancellationToken);

        return count > 0;
    }

    public Task<SpcInspectionEntity?> GetInspectionAsync(string equipmentId, string processCode, CancellationToken cancellationToken)
    {
        return QuerySingleAsync<SpcInspectionEntity?>(
            @"BEGIN
                CORONAVTR.P_SPC_CHART_CHECK_PROCESSCODE(:EquipmentId,:ProcessCode);
              END;",
            new { EquipmentId = equipmentId, ProcessCode = processCode },
            cancellationToken);
    }
}
