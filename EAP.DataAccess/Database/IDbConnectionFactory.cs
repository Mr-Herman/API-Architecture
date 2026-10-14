using System.Data.Common;

namespace EAP.DataAccess.Database;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection(string connectionName);

    Task<DbConnection> OpenConnectionAsync(string connectionName, CancellationToken cancellationToken = default);

    int GetCommandTimeoutSeconds(string connectionName);
}
