using System.Security.Cryptography;
using System.Text;

namespace Hhs.AuthServer.Store;

public static class HashExtensions
{
    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string Sha256(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        using (var sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash.</returns>
    public static byte[] Sha256(this byte[] input)
    {
        if (input == null)
        {
            return null;
        }

        using (var sha = SHA256.Create())
        {
            return sha.ComputeHash(input);
        }
    }

    /// <summary>
    /// Creates a SHA512 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string Sha512(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        using (var sha = SHA512.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}