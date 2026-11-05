using System.Security.Cryptography;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;

namespace Hsx.IdentityService.Application.Security;

/// <summary>
/// Makes the first password of a user: random, from a cryptographic source, and good for the rules of the tenant (<see cref="PasswordRules"/>), so the user is not refused by its own password. It is read from a message (an email or a text), so the letters
/// and the digits that look alike (<c>O 0 I l 1</c>) are left out. No person knows it: it is made, hashed, sent and forgotten.
/// </summary>
public static class TemporaryPasswordGenerator
{
    /// <summary>A password is never shorter than this, whatever the rules say.</summary>
    public const int MinimumLength = 14;

    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!#$%&*+-=?@";

    public static string Generate(PasswordRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        int length = Math.Max(MinimumLength, rules.MinLength);
        // every kind the rules want is in it, then the rest is drawn from all the kinds the rules allow
        List<char> chars = [Pick(Lower), Pick(Upper), Pick(Digits)];
        string all = Lower + Upper + Digits;
        if (rules.RequireNonAlphanumeric)
        {
            chars.Add(Pick(Symbols));
            all += Symbols;
        }

        while (chars.Count < length)
        {
            chars.Add(Pick(all));
        }

        // the first characters are the required kinds: shuffle, so their place says nothing
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string([.. chars]);
    }

    private static char Pick(string from) => from[RandomNumberGenerator.GetInt32(from.Length)];
}
