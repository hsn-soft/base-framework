using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

/// <summary>
/// WEB_PLATFORM
/// PODCAST
/// EFATURA
/// </summary>
public sealed class ProductType : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    private ProductType()
    {
    }

    public ProductType(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }
}