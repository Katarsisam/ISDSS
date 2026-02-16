using System.Security.Cryptography;
using System.Text;

namespace ISDSS.Infrastructure.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        if (password is null)
            throw new ArgumentNullException(nameof(password));

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string hash) =>
        string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
}
