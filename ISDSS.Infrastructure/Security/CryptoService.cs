using System.Linq;
using System.Security.Cryptography;
using ISDSS.Application.Abstractions;

namespace ISDSS.Infrastructure.Security;

/// <summary>
/// Шифрование/дешифрование для экспорта студентов.
/// На Windows ключ дополнительно защищён через DPAPI, на других ОС хранится в файле как есть.
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly string _keyPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ISDSS", "key.bin");

    private byte[] GetOrCreateKey()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_keyPath)!);
        if (!File.Exists(_keyPath))
        {
            var raw = RandomNumberGenerator.GetBytes(32); // 256-bit
            File.WriteAllBytes(_keyPath, Protect(raw));
            return raw;
        }

        var stored = File.ReadAllBytes(_keyPath);
        return Unprotect(stored);
    }

    private static byte[] Protect(byte[] data)
        => OperatingSystem.IsWindows()
            ? ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser)
            : data;

    private static byte[] Unprotect(byte[] data)
        => OperatingSystem.IsWindows()
            ? ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser)
            : data;

    public byte[] Encrypt(byte[] data)
    {
        var key = GetOrCreateKey();
        using var aes = new AesGcm(key, 16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ct = new byte[data.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, data, ct, tag);
        return nonce.Concat(tag).Concat(ct).ToArray();
    }

    public byte[] Decrypt(byte[] data)
    {
        var key = GetOrCreateKey();
        using var aes = new AesGcm(key, 16);
        var nonce = data.AsSpan(0, 12).ToArray();
        var tag   = data.AsSpan(12, 16).ToArray();
        var ct    = data.AsSpan(28).ToArray();
        var pt = new byte[ct.Length];
        aes.Decrypt(nonce, ct, tag, pt);
        return pt;
    }
}
