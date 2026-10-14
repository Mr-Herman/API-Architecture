using System.Security.Cryptography;
using System.Text;
using EAP.DataAccess.Options;
using Microsoft.Extensions.Options;

namespace EAP.DataAccess.Security;

public sealed class AesConnectionStringProtector : IConnectionStringProtector
{
    public const string EncryptedPrefix = "enc:v1:";

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly byte[] AdditionalData =
        Encoding.UTF8.GetBytes("EAP.Database.ConnectionString.v1");

    private readonly byte[] _key;

    public AesConnectionStringProtector(IOptions<EncryptionOptions> options)
    {
        var encodedKey = File.ReadAllText(options.Value.KeyFilePath).Trim();
        _key = Convert.FromBase64String(encodedKey);

        if (_key.Length != KeySize)
        {
            throw new InvalidOperationException("AES key must contain exactly 32 bytes.");
        }
    }

    public string Encrypt(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        var plaintext = Encoding.UTF8.GetBytes(connectionString);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, cipherText, tag, AdditionalData);

        var payload = new byte[NonceSize + TagSize + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, tag.Length);
        Buffer.BlockCopy(cipherText, 0, payload, NonceSize + TagSize, cipherText.Length);

        return EncryptedPrefix + Convert.ToBase64String(payload);
    }

    public string Decrypt(string protectedConnectionString)
    {
        ArgumentNullException.ThrowIfNull(protectedConnectionString);

        if (!protectedConnectionString.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return protectedConnectionString;
        }

        try
        {
            var payload = Convert.FromBase64String(protectedConnectionString[EncryptedPrefix.Length..]);
            if (payload.Length < NonceSize + TagSize)
            {
                throw new InvalidOperationException("Encrypted database connection string is invalid.");
            }

            var nonce = payload[..NonceSize];
            var tag = payload[NonceSize..(NonceSize + TagSize)];
            var cipherText = payload[(NonceSize + TagSize)..];
            var plaintext = new byte[cipherText.Length];

            using var aes = new AesGcm(_key, TagSize);
            try
            {
                aes.Decrypt(nonce, cipherText, tag, plaintext, AdditionalData);
            }
            catch (AuthenticationTagMismatchException)
            {
                Array.Clear(plaintext);
                aes.Decrypt(nonce, cipherText, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Database connection string could not be decrypted.", exception);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("Database connection string could not be decrypted.", exception);
        }
    }
}
