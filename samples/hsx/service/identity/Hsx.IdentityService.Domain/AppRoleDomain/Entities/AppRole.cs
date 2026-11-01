using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;

namespace Hsx.IdentityService.Domain.AppRoleDomain.Entities;

/// <summary>
/// A role of a tenant. The subscriptions a role has (<c>AppRoleSubscription</c>) are what its users may see of the products: the scope keys of those subscriptions go into their token. The tenant is kept here and given by the persistence.
/// </summary>
public sealed class AppRole : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRole()
    {
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>A role the system made, which is not deleted.</summary>
    public bool IsStatic { get; private set; }

    public static AppRole Create(string name, bool isStatic = false)
    {
        AppRole role = new() { Id = Guid.CreateVersion7(), IsStatic = isStatic };
        role.Update(name);
        return role;
    }

    public void Update(string name)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), AppRoleConsts.NameMaxLength).Trim();
        NormalizedName = StringHelper.Normalize(Name);
    }
}
