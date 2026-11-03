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

    /// <summary>Which subscriptions the users of the role see; fixed when the role is made.</summary>
    public AppRoleType RoleType { get; private set; }

    /// <summary>The company a <see cref="AppRoleType.Company"/> role names; nothing for any other type.</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>The customer a <see cref="AppRoleType.Customer"/> role names; nothing for any other type.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>A role of a type. A <see cref="AppRoleType.Company"/> role names a company and a <see cref="AppRoleType.Customer"/> role a customer; no other type names either.</summary>
    /// <exception cref="Exceptions.AppRoleTypeInvalidException">The type and what it names do not go together.</exception>
    public static AppRole Create(string name, AppRoleType roleType, Guid? companyId = null, Guid? customerId = null, bool isStatic = false)
    {
        if (!Enum.IsDefined(roleType))
        {
            throw new Exceptions.AppRoleTypeInvalidException($"'{roleType}' is not a type of role.");
        }

        if ((roleType == AppRoleType.Company) != (companyId is { } company && company != Guid.Empty))
        {
            throw new Exceptions.AppRoleTypeInvalidException(roleType == AppRoleType.Company ? "A company role names a company." : "Only a company role names a company.");
        }

        if ((roleType == AppRoleType.Customer) != (customerId is { } customer && customer != Guid.Empty))
        {
            throw new Exceptions.AppRoleTypeInvalidException(roleType == AppRoleType.Customer ? "A customer role names a customer." : "Only a customer role names a customer.");
        }

        AppRole role = new() { Id = Guid.CreateVersion7(), IsStatic = isStatic, RoleType = roleType, CompanyId = companyId, CustomerId = customerId };
        role.Update(name);
        return role;
    }

    public void Update(string name)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), AppRoleConsts.NameMaxLength).Trim();
        NormalizedName = StringHelper.Normalize(Name);
    }
}
