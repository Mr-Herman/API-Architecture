using System.Data;
using EAP.DataAccess.Database.SqlServer;
using Microsoft.Data.SqlClient;

namespace EAP.UnitTests.Tests;

public sealed class SqlServerBulkWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenDestinationContainsSql_Throws()
    {
        var writer = new SqlServerBulkWriter();
        using var connection = new SqlConnection();
        var data = CreateEmptyTable();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            writer.WriteAsync(connection, null, "dbo.Target; DROP TABLE Users", data));

        Assert.Contains("trusted SQL identifier", exception.Message);
    }

    [Fact]
    public async Task WriteAsync_WhenSourceColumnIsMissing_Throws()
    {
        var writer = new SqlServerBulkWriter();
        using var connection = new SqlConnection();
        var data = CreateEmptyTable();
        var mappings = new Dictionary<string, string>
        {
            ["MissingColumn"] = "TARGET_ID"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            writer.WriteAsync(connection, null, "dbo.TargetTable", data, mappings));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task WriteAsync_WhenTableIsEmpty_DoesNotRequireOpenConnection()
    {
        var writer = new SqlServerBulkWriter();
        using var connection = new SqlConnection();
        var data = CreateEmptyTable();

        await writer.WriteAsync(connection, null, "dbo.TargetTable", data);
    }

    private static DataTable CreateEmptyTable()
    {
        var data = new DataTable();
        data.Columns.Add("ID", typeof(long));
        return data;
    }
}
