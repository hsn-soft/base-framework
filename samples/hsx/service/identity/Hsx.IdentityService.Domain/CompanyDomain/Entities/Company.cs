using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;

namespace Hsx.IdentityService.Domain.CompanyDomain.Entities;

/// <summary>
/// A company that has customers (it is not under a tenant: the tenants that sell it something are named by its subscriptions).
/// </summary>
public sealed class Company : FullAuditedEntity<Guid>
{
    // for EF Core
    private Company()
    {
    }

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

    public void Update(string title)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), CompanyConsts.TitleMaxLength).Trim();
    }
}
