using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tunarrly.Core.Options;

namespace Tunarrly.Infrastructure.Settings;

public sealed class SecretProtector(IOptions<SecretsOptions> options)
{
    private const string Prefix = "enc:v1:";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(options.Value.EncryptionKey);

    public bool IsProtected(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value) || !IsEnabled || IsProtected(value)) return value;

        var key = DeriveKey(options.Value.EncryptionKey);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return string.Join(':', Prefix.TrimEnd(':'), Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext));
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value) || !IsProtected(value)) return value;
        if (!IsEnabled) return string.Empty;

        var parts = value.Split(':', 5);
        if (parts.Length != 5) return string.Empty;

        try
        {
            var key = DeriveKey(options.Value.EncryptionKey);
            var nonce = Convert.FromBase64String(parts[2]);
            var tag = Convert.FromBase64String(parts[3]);
            var ciphertext = Convert.FromBase64String(parts[4]);
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return string.Empty;
        }
    }

    private static byte[] DeriveKey(string keyMaterial) => SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
}
