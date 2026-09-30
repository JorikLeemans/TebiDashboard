using System.Security.Cryptography;
using System.Text;

namespace TebiDashboard.Infrastructure.Services;

/// <summary>
/// PBKDF2 password hashes. Stored form: pbkdf2-sha256$iterations$salt$hash.
/// A value without that prefix is a legacy plaintext password and is upgraded after a successful login.
/// </summary>
internal static class PasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>Creates a new salted hash for a password that already passed length checks.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Returns true when the password matches. When a legacy plaintext value matches,
    /// <paramref name="upgradedHash"/> is a new PBKDF2 hash the caller should store.
    /// </summary>
    public static bool Verify(string? password, string? stored, out string? upgradedHash)
    {
        upgradedHash = null;
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
        {
            return false;
        }

        if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            if (!FixedEquals(password, stored))
            {
                return false;
            }

            upgradedHash = Hash(password);
            return true;
        }

        var parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 1)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);

    /// <summary>Compares two strings without returning early on the first different byte.</summary>
    private static bool FixedEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
