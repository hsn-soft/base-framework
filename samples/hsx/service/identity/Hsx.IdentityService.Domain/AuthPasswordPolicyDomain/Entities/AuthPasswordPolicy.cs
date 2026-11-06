using HsnSoft.Base;
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

    public bool RequireLowercase { get; private set; }

    public bool RequireUppercase { get; private set; }

    public bool RequireNonAlphanumeric { get; private set; }

    public int MaxFailedLoginCount { get; private set; }

    public int LockoutMinutes { get; private set; }

    public static AuthPasswordPolicy Create(PasswordRules rules, Guid? id = null)
    {
        AuthPasswordPolicy policy = new() { Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7() };
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
        RequireLowercase = rules.RequireLowercase;
        RequireUppercase = rules.RequireUppercase;
        RequireNonAlphanumeric = rules.RequireNonAlphanumeric;
        MaxFailedLoginCount = rules.MaxFailedLoginCount;
        LockoutMinutes = rules.LockoutMinutes;
    }

    public PasswordRules ToRules() => new(MinLength, RequireDigit, RequireLowercase, RequireUppercase, RequireNonAlphanumeric, MaxFailedLoginCount, LockoutMinutes);
}
