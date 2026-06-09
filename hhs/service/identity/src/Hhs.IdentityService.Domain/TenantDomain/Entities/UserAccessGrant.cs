using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class UserAccessGrant : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProductSubscriptionId { get; private set; }

    public ProductSubscription ProductSubscription { get; private set; } = null!;

    public bool IsActive { get; private set; }

    private UserAccessGrant()
    {
    }

    public UserAccessGrant(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid productSubscriptionId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        ProductSubscriptionId = productSubscriptionId;
        IsActive = true;
    }

    public void Passive()
    {
        IsActive = false;
    }
}

