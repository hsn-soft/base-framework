using Edx.IdentityService.Domain.IdentityDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>
/// A company that uses the system, or the system itself. It is not <c>IMultiTenant</c>: it is what the tenants are, so there is no tenant filter on it; the application service lets a user of a customer see only its own
/// row, and the users of the system tenant see them all. The id is chosen by the caller (the ids of the sample are constants of <c>Edx.Shared.Helper</c>).
/// </summary>
public sealed class Tenant : Entity<Guid>
{
    // for EF Core
    private Tenant()
    {
    }

    /// <summary>What a user names its tenant by when it signs in (<c>veriban</c>): in small letters, unique.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    /// <summary>The tenant this one is under (its customer relation), or nothing for a tenant at the root.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>Where the tenant is in the tree: the ids from the root down to it, <c>/{root}/{parent}/{tenant}/</c> (<see cref="TenantAccessPath"/>). The tenants a tenant may see are the ones whose path starts with its own.</summary>
    public string AccessPath { get; private set; } = string.Empty;

    public static Tenant Create(Guid id, string code, string name, Tenant? parent = null)
    {
        Tenant tenant = new()
        {
            Id = Guard.NotDefault<Guid>(id, nameof(Id)),
            Code = Guard.NotNullOrWhiteSpace(StringHelper.Normalize(Guard.NotNullOrWhiteSpace(code, nameof(Code), IdentityConsts.CodeMaxLength)), nameof(Code)).ToLowerInvariant(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), IdentityConsts.NameMaxLength),
            IsActive = true,
        };
        tenant.PlaceUnder(parent);
        return tenant;
    }

    /// <summary>Sets the path of a tenant that moved with its parent (<see cref="TenantAccessPath.Move"/>).</summary>
    public void SetAccessPath(string path)
        => AccessPath = TenantAccessPath.IsValid(path) ? path : throw new ArgumentException($"'{path}' is not the path of a tenant.", nameof(path));

    /// <summary>Puts the tenant under another (or at the root): sets its parent and its path. The tenants under it keep their old paths until <see cref="TenantAccessPath.Move"/> is applied to them, so the caller moves them in the same step.</summary>
    public void PlaceUnder(Tenant? parent)
    {
        ParentId = parent?.Id;
        AccessPath = parent is null ? TenantAccessPath.Root(Id) : TenantAccessPath.Child(parent.AccessPath, Id);
    }
}
