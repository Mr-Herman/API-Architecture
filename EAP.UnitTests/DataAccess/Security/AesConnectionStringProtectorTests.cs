using System.Security.Cryptography;
using System.Text;
using EAP.DataAccess.Options;
using EAP.DataAccess.Security;
using Microsoft.Extensions.Options;

namespace EAP.UnitTests.Tests;

public sealed class AesConnectionStringProtectorTests : IDisposable
{
    private readonly string _keyFilePath;

    public AesConnectionStringProtectorTests()
    {
        _keyFilePath = Path.Combine(Path.GetTempPath(), $"eap-aes-key-{Guid.NewGuid():N}.txt");

        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(_keyFilePath, Convert.ToBase64String(key));
    }

    [Fact]
    public void EncryptAndDecrypt_ReturnsOriginalConnectionString()
    {
        var protector = CreateProtector();
        const string connectionString = "Server=localhost;Database=test;User Id=user;Password=password;";

        var encrypted = protector.Encrypt(connectionString);
        var decrypted = protector.Decrypt(encrypted);

        Assert.StartsWith(AesConnectionStringProtector.EncryptedPrefix, encrypted, StringComparison.Ordinal);
        Assert.DoesNotContain("password", encrypted, StringComparison.Ordinal);
        Assert.Equal(connectionString, decrypted);
    }

    [Fact]
    public void Decrypt_WhenCipherTextWasModified_Throws()
    {
        var protector = CreateProtector();
        var encrypted = protector.Encrypt("Server=localhost;Password=password;");
        var replacement = encrypted.EndsWith('A') ? 'B' : 'A';
        var modified = encrypted[..^1] + replacement;

        var exception = Assert.Throws<InvalidOperationException>(() => protector.Decrypt(modified));

        Assert.Contains("could not be decrypted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decrypt_WhenKeyIsDifferent_Throws()
    {
        var originalProtector = CreateProtector();
        var encrypted = originalProtector.Encrypt(
            "Server=localhost;Password=password;");

        var differentKey = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(
            _keyFilePath,
            Convert.ToBase64String(differentKey));
        var differentProtector = CreateProtector();

        var exception = Assert.Throws<InvalidOperationException>(() => differentProtector.Decrypt(encrypted));

        Assert.Contains("could not be decrypted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decrypt_WhenValueIsNotEncrypted_ReturnsOriginalValue()
    {
        var protector = CreateProtector();
        const string connectionString = "Server=localhost;Database=test;";

        var result = protector.Decrypt(connectionString);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void Decrypt_WhenCipherTextUsesLegacyFormat_ReturnsOriginalConnectionString()
    {
        const string connectionString = "Server=localhost;Database=legacy;Password=password;";
        var encrypted = EncryptWithoutAdditionalData(connectionString);
        var protector = CreateProtector();

        var decrypted = protector.Decrypt(encrypted);

        Assert.Equal(connectionString, decrypted);
    }

    public void Dispose()
    {
        if (File.Exists(_keyFilePath))
        {
            File.Delete(_keyFilePath);
        }
    }

    private AesConnectionStringProtector CreateProtector()
    {
        var options = Options.Create(new EncryptionOptions
        {
            KeyFilePath = _keyFilePath
        });

        return new AesConnectionStringProtector(options);
    }

    private string EncryptWithoutAdditionalData(string connectionString)
    {
        const int nonceSize = 12;
        const int tagSize = 16;
        var key = Convert.FromBase64String(File.ReadAllText(_keyFilePath).Trim());
        var plaintext = Encoding.UTF8.GetBytes(connectionString);
        var nonce = RandomNumberGenerator.GetBytes(nonceSize);
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[tagSize];

        using var aes = new AesGcm(key, tagSize);
        aes.Encrypt(nonce, plaintext, cipherText, tag);

        var payload = new byte[nonceSize + tagSize + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonceSize, tag.Length);
        Buffer.BlockCopy(cipherText, 0, payload, nonceSize + tagSize, cipherText.Length);

        return AesConnectionStringProtector.EncryptedPrefix
            + Convert.ToBase64String(payload);
    }
}
