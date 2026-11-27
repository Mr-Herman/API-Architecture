namespace EAP.Infrastructure.Options;

public sealed class LegacyPasswordOptions
{
    public const string SectionName = "LegacyPassword";

    public string Key { get; set; } = string.Empty;
}
