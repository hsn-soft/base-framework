namespace Hsx.IdentityService.Application.Security;

/// <summary>How long the temporary password of a new user is good for (the section <c>Identity:TemporaryPassword</c>).</summary>
public sealed class TemporaryPasswordOptions
{
    public const string SectionName = "Identity:TemporaryPassword";

    /// <summary>After this time the password signs in no more, and an administrator asks for a new one. 24 hours when it is not set.</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// <b>Only for a sample that has no notification service.</b> When it is given, every temporary password is this one instead of a random one, so the people of the sample (and its smoke test) can sign in with a user the API made: in a real system the password
    /// is read by nobody but the user it is sent to. Never set it in a system of people.
    /// </summary>
    public string? SamplePassword { get; set; }
}
