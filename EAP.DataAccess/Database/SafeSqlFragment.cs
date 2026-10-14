namespace EAP.DataAccess.Database;

public static class SafeSqlFragment
{
    public static string FromWhitelist(string requestedValue, IReadOnlyDictionary<string, string> allowedFragments, string parameterName)
    {
        foreach (var fragment in allowedFragments)
        {
            var matched = string.Equals(fragment.Key, requestedValue, StringComparison.OrdinalIgnoreCase);
            if (matched)
            {
                return fragment.Value;
            }
        }

        throw new ArgumentException($"Unsupported SQL option '{requestedValue}'.", parameterName);
    }
}
