using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;

namespace Hsx.IdentityService.Domain.CompanyDomain.Entities;

/// <summary>
/// A company that has customers. It is part of the catalogue of the system, written by the system tenant: it belongs to no tenant and has no filter, and it is not a tenant, so it has no users or roles of its own (the roles of a reseller that name it do).
/// </summary>
public sealed class Company : AuditedEntity<Guid>, ISoftDelete
{
    // for EF Core
    private Company()
    {
    }

    public bool IsDeleted { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded

    public static Company Create(string title, string name, Guid? id = null)
    {
        Company entity = new()
        {
            Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), CompanyConsts.NameMaxLength).Trim()
        };
        entity.NormalizedName = Guard.NotNullOrWhiteSpace(StringHelper.Normalize(entity.Name), nameof(NormalizedName), CompanyConsts.NameMaxLength);
        entity.Update(title);
        return entity;
    }

    public void Update(string title)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), CompanyConsts.TitleMaxLength).Trim();
    }
}
