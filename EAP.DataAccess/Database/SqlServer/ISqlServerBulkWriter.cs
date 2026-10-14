using System.Data;
using Microsoft.Data.SqlClient;

namespace EAP.DataAccess.Database.SqlServer;

public interface ISqlServerBulkWriter
{
    Task WriteAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string destinationTable,
        DataTable data,
        IReadOnlyDictionary<string, string>? columnMappings = null,
        int batchSize = 5000,
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<int> MergeAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string createStagingTableSql,
        string stagingTableName,
        string mergeSql,
        DataTable data,
        IReadOnlyDictionary<string, string>? columnMappings = null,
        int batchSize = 5000,
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default);
}
