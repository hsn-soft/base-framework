using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

/// <summary>
/// CINER HOLDING
/// ILBAK HOLDING
/// </summary>
public sealed class Holding : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    private Holding()
    {
    }

    public Holding(Guid id, string name, string normalizedName)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }
}