using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace EAP.DataAccess.Database.SqlServer;

public sealed class SqlServerBulkWriter : ISqlServerBulkWriter
{
    private const string ValidIdentifierPattern = "^#?[A-Za-z_][A-Za-z0-9_]*$";

    public async Task WriteAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string destinationTable,
        DataTable data,
        IReadOnlyDictionary<string, string>? columnMappings = null,
        int batchSize = 5000,
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(data);
        ValidateDestinationTable(destinationTable);
        ValidateSettings(batchSize, timeoutSeconds);

        var mappings = ResolveMappings(data, columnMappings);
        if (data.Rows.Count == 0)
        {
            return;
        }

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("SQL Server connection must be open before BulkCopy starts.");
        }

        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = destinationTable,
            BatchSize = batchSize,
            BulkCopyTimeout = timeoutSeconds,
            EnableStreaming = true
        };

        foreach (var mapping in mappings)
        {
            bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
        }

        await bulkCopy.WriteToServerAsync(data, cancellationToken);
    }

    public async Task<int> MergeAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string createStagingTableSql,
        string stagingTableName,
        string mergeSql,
        DataTable data,
        IReadOnlyDictionary<string, string>? columnMappings = null,
        int batchSize = 5000,
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(data);
        ValidateSql(createStagingTableSql, nameof(createStagingTableSql));
        ValidateSql(mergeSql, nameof(mergeSql));
        ValidateDestinationTable(stagingTableName);
        ValidateSettings(batchSize, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("SQL Server connection must be open before BulkMerge starts.");
        }

        if (!ReferenceEquals(transaction.Connection, connection))
        {
            throw new InvalidOperationException("BulkMerge transaction must belong to the supplied SQL Server connection.");
        }

        await ExecuteCommandAsync(connection, transaction, createStagingTableSql, timeoutSeconds, cancellationToken);

        await WriteAsync(connection, transaction, stagingTableName, data, columnMappings, batchSize, timeoutSeconds, cancellationToken);

        return await ExecuteCommandAsync(connection, transaction, mergeSql, timeoutSeconds, cancellationToken);
    }

    private static async Task<int> ExecuteCommandAsync(SqlConnection connection, SqlTransaction transaction, string sql, int timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = timeoutSeconds
        };

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> ResolveMappings(DataTable data, IReadOnlyDictionary<string, string>? columnMappings)
    {
        if (columnMappings is null || columnMappings.Count == 0)
        {
            return data.Columns
                .Cast<DataColumn>()
                .ToDictionary(column => column.ColumnName, column => column.ColumnName);
        }

        foreach (var mapping in columnMappings)
        {
            if (!data.Columns.Contains(mapping.Key))
            {
                throw new ArgumentException($"BulkCopy source column '{mapping.Key}' does not exist.", nameof(columnMappings));
            }

            ValidateIdentifier(mapping.Value, nameof(columnMappings));
        }

        return columnMappings;
    }

    private static void ValidateDestinationTable(string destinationTable)
    {
        if (string.IsNullOrWhiteSpace(destinationTable))
        {
            throw new ArgumentException("BulkCopy destination table is required.", nameof(destinationTable));
        }

        var identifiers = destinationTable.Split('.');
        if (identifiers.Length is < 1 or > 2 || identifiers.Any(identifier => !IsValidIdentifier(identifier)))
        {
            throw new ArgumentException($"BulkCopy destination table '{destinationTable}' is not a trusted SQL identifier.", nameof(destinationTable));
        }
    }

    private static void ValidateSql(string sql, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL Server BulkMerge SQL is required.", parameterName);
        }
    }

    private static void ValidateSettings(int batchSize, int timeoutSeconds)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "BulkCopy batch size must be greater than zero.");
        }

        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "BulkCopy timeout must be greater than zero.");
        }
    }

    private static void ValidateIdentifier(string identifier, string parameterName)
    {
        if (!IsValidIdentifier(identifier))
        {
            throw new ArgumentException($"SQL identifier '{identifier}' is not allowed.", parameterName);
        }
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        return Regex.IsMatch(identifier, ValidIdentifierPattern, RegexOptions.CultureInvariant);
    }
}
