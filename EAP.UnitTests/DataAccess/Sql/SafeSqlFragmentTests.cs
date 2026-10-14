using EAP.DataAccess.Database;

namespace EAP.UnitTests.Tests;

public sealed class SafeSqlFragmentTests
{
    private static readonly IReadOnlyDictionary<string, string> AllowedSortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "createdAt", "CREATE_TIME" },
            { "name", "EQUIPMENT_NAME" }
        };

    [Fact]
    public void FromWhitelist_WhenClientValueIsAllowed_ReturnsDatabaseColumn()
    {
        const string clientSortValue = "NAME";
        var databaseColumn = SafeSqlFragment.FromWhitelist(clientSortValue, AllowedSortColumns, "sortBy");

        Assert.Equal("EQUIPMENT_NAME", databaseColumn);
    }

    [Fact]
    public void FromWhitelist_WhenClientValueIsNotAllowed_ThrowsArgumentException()
    {
        const string unsafeClientValue = "name; DROP TABLE Equipment";
        void SelectDatabaseColumn()
        {
            SafeSqlFragment.FromWhitelist(unsafeClientValue, AllowedSortColumns, "sortBy");
        }
        var exception = Assert.Throws<ArgumentException>(SelectDatabaseColumn);

        Assert.Equal("sortBy", exception.ParamName);
    }
}
