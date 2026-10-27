using Edx.IdentityService.Domain.IdentityDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A role of a tenant (<c>admin</c>, <c>operator</c>): each tenant has roles of its own, and the name only has to be unique in it. The tenant is kept here and given by the persistence.</summary>
public sealed class Role : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private Role()
    {
    }

    /// <summary>The name in small letters: it becomes a <c>role</c> claim of the token.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The name in capitals: what is unique in a tenant.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static Role Create(string name)
    {
        string text = Guard.NotNullOrWhiteSpace(name, nameof(Name), IdentityConsts.NameMaxLength).ToLowerInvariant();
        return new() { Id = Guid.CreateVersion7(), Name = text, NormalizedName = StringHelper.Normalize(text) };
    }
}
