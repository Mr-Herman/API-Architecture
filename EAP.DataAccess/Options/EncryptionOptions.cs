namespace EAP.DataAccess.Options;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    public string KeyFilePath { get; set; } = string.Empty;
}
