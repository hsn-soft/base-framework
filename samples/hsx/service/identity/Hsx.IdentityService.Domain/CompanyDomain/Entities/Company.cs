using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;

namespace Hsx.IdentityService.Domain.CompanyDomain.Entities;

/// <summary>
/// A company that has customers. It belongs to the tenant that made it (the reseller; the tenant is kept here and given by the persistence), which sees it with the tenants under it. A company made through tenant management also has an account tenant of its own
/// (<see cref="AccountTenantId"/>): the tenant its people sign in to.
/// </summary>
public sealed class Company : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private Company()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>The tenant of the company's own people, under the reseller that made the company; nothing for a company made without one. An explicit link: hhs gave the tenant the id of the company.</summary>
    public Guid? AccountTenantId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;

    public static Company Create(string title, string name)
    {
        Company entity = new()
        {
            Id = Guid.CreateVersion7(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), CompanyConsts.NameMaxLength).Trim()
        };
        entity.NormalizedName = StringHelper.Normalize(entity.Name);
        entity.Update(title);
        return entity;
    }

    public void LinkAccountTenant(Guid accountTenantId) => AccountTenantId = Guard.NotDefault<Guid>(accountTenantId, nameof(AccountTenantId));

    public void Update(string title)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), CompanyConsts.TitleMaxLength).Trim();
    }
}
