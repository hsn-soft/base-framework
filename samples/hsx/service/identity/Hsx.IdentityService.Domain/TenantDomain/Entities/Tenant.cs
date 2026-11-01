using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Domain.TenantDomain.Entities;

/// <summary>
/// The system itself, a reseller that sells the products, or an account under a reseller. The tenants are a tree: a tenant sees its own data and the data of the tenants under it. It is not <c>IMultiTenant</c>: it is what the tenants are, so
/// there is no tenant filter on it; the application service shows a tenant the ones under it, and the system tenant all of them. The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>.
/// </summary>
public sealed class Tenant : FullAuditedEntity<Guid>
{
    // for EF Core
    private Tenant()
    {
    }

    public TenantTypes TenantType { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>What a tenant is called in the system (small letters, unique).</summary>
    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    /// <summary>The tenant this one is under, or nothing for a tenant at the root.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>Where the tenant is in the tree: the ids from the root down to it, <c>/{root}/{parent}/{tenant}/</c> (<see cref="TenantAccessPath"/>). The tenants a tenant may see are the ones whose path starts with its own.</summary>
    public string AccessPath { get; private set; } = string.Empty;

    public static Tenant Create(Guid? id, TenantTypes tenantType, string title, string name, Tenant? parent = null)
    {
        Tenant tenant = new()
        {
            Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(),
            TenantType = tenantType == TenantTypes.Unknown ? throw new ArgumentException("The type of a tenant is not given.", nameof(tenantType)) : tenantType,
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), TenantConsts.NameMaxLength).Trim().ToLowerInvariant(),
            IsActive = true,
        };
        tenant.NormalizedName = StringHelper.Normalize(tenant.Name);
        tenant.Update(title, true);
        tenant.PlaceUnder(parent);
        return tenant;
    }

    public void Update(string title, bool isActive)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), TenantConsts.TitleMaxLength).Trim();
        IsActive = isActive;
    }

    /// <summary>Puts the tenant under another (or at the root): sets its parent and its path. The tenants under it keep their old paths until <see cref="TenantAccessPath.Move"/> is applied to them, so the caller moves them in the same step.</summary>
    public void PlaceUnder(Tenant? parent)
    {
        ParentId = parent?.Id;
        AccessPath = parent is null ? TenantAccessPath.Root(Id) : TenantAccessPath.Child(parent.AccessPath, Id);
    }

    /// <summary>Sets the path of a tenant that moved with its parent (<see cref="TenantAccessPath.Move"/>).</summary>
    public void SetAccessPath(string path)
        => AccessPath = TenantAccessPath.IsValid(path) ? path : throw new ArgumentException($"'{path}' is not the path of a tenant.", nameof(path));
}
