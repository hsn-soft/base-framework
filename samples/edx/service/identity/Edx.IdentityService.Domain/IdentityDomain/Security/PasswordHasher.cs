using System.Security.Cryptography;

namespace Edx.IdentityService.Domain.IdentityDomain.Security;

/// <summary>
/// Hashes passwords (PBKDF2 with SHA-256, a random salt per password) and checks them. The hash is one text, <c>v1.{iterations}.{salt}.{hash}</c>, so the cost can be raised later and
/// old hashes still check. The identity service writes the hashes and the auth server reads them, so both use this one class.
/// </summary>
public static class PasswordHasher
{
    private const string Version = "v1";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Version}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>Whether <paramref name="password"/> is the one <paramref name="passwordHash"/> was made from; a text that is not a hash of this class is never a match.</summary>
    public static bool Verify(string? passwordHash, string password)
    {
        string[] parts = passwordHash?.Split('.') ?? [];
        if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out int iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
