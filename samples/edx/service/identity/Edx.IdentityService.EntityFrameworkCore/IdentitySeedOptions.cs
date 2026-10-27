namespace Edx.IdentityService.EntityFrameworkCore;

/// <summary>The sample users that are made when the database has none (section <c>Seed</c>, in <c>appsettings.development.json</c> only): the passwords are the configuration's, never the code's.</summary>
public sealed class IdentitySeedOptions
{
    public const string SectionName = "Seed";

    public List<SeedUser> Users { get; set; } = [];
}

/// <summary>A user to make: in the tenant of that code, with a role of its own name.</summary>
public sealed class SeedUser
{
    /// <summary>The code of the tenant (<c>system-tenant</c>, <c>veriban</c>, <c>bilgera</c>).</summary>
    public string Tenant { get; set; } = string.Empty;

    /// <summary>The user name; the role of the user is named the same.</summary>
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
