using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;

/// <summary>The password policy a tenant has chosen; the tenant is kept here and given by the persistence. A tenant has at most one.</summary>
public sealed class AuthPasswordPolicy : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AuthPasswordPolicy()
    {
    }

    public Guid TenantId { get; private set; }

    public int MinLength { get; private set; }

    public bool RequireDigit { get; private set; }

    public bool RequireLower { get; private set; }

    public bool RequireUpper { get; private set; }

    public bool RequireNonAlphanumeric { get; private set; }

    public int MaxFailedLoginCount { get; private set; }

    public int LockoutMinutes { get; private set; }

    public static AuthPasswordPolicy Create(PasswordRules rules)
    {
        AuthPasswordPolicy policy = new() { Id = Guid.CreateVersion7() };
        policy.Change(rules);
        return policy;
    }

    public void Change(PasswordRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentOutOfRangeException.ThrowIfLessThan(rules.MinLength, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rules.MaxFailedLoginCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rules.LockoutMinutes, 1);

        MinLength = rules.MinLength;
        RequireDigit = rules.RequireDigit;
        RequireLower = rules.RequireLower;
        RequireUpper = rules.RequireUpper;
        RequireNonAlphanumeric = rules.RequireNonAlphanumeric;
        MaxFailedLoginCount = rules.MaxFailedLoginCount;
        LockoutMinutes = rules.LockoutMinutes;
    }

    public PasswordRules ToRules() => new(MinLength, RequireDigit, RequireLower, RequireUpper, RequireNonAlphanumeric, MaxFailedLoginCount, LockoutMinutes);
}
