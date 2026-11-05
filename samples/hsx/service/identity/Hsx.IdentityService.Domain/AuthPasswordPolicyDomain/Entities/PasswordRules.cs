namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;

/// <summary>
/// What a tenant asks of a password and of a sign-in: the length and the kinds of characters, how many failed sign-ins lock the user and for how long. A tenant that has no policy of its own has <see cref="Default"/>.
/// </summary>
public sealed record PasswordRules(int MinLength, bool RequireDigit, bool RequireLowercase, bool RequireUppercase, bool RequireNonAlphanumeric, int MaxFailedLoginCount, int LockoutMinutes)
{
    public static PasswordRules Default { get; } = new(8, true, true, true, false, 5, 15);

    /// <summary>What the password lacks, as the codes of the rules it breaks (<c>min-length</c>, <c>digit</c>, <c>lower</c>, <c>upper</c>, <c>non-alphanumeric</c>); empty when it keeps them all.</summary>
    public IReadOnlyList<string> Check(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        List<string> broken = [];
        if (password.Length < MinLength)
        {
            broken.Add("min-length");
        }

        if (RequireDigit && !password.Any(char.IsDigit))
        {
            broken.Add("digit");
        }

        if (RequireLowercase && !password.Any(char.IsLower))
        {
            broken.Add("lower");
        }

        if (RequireUppercase && !password.Any(char.IsUpper))
        {
            broken.Add("upper");
        }

        if (RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            broken.Add("non-alphanumeric");
        }

        return broken;
    }
}
