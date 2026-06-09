using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class ProductSubscription : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid HoldingId { get; private set; }

    public Holding Holding { get; private set; } = null!;

    public Guid ClientId { get; private set; }

    public ClientNew Client { get; private set; } = null!;

    public Guid ProductTypeId { get; private set; }

    public ProductType ProductType { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime ValidFrom { get; private set; }

    public DateTime? ValidTo { get; private set; }

    public string SettingsJson { get; private set; } = "{}";

    private ProductSubscription()
    {
    }

    public ProductSubscription(
        Guid id,
        Guid tenantId,
        Guid holdingId,
        Guid clientId,
        Guid productTypeId,
        string settingsJson = "{}")
    {
        Id = id;
        TenantId = tenantId;
        HoldingId = holdingId;
        ClientId = clientId;
        ProductTypeId = productTypeId;
        SettingsJson = settingsJson;
        IsActive = true;
        ValidFrom = DateTime.UtcNow;
    }

    public void Passive(DateTime? validTo = null)
    {
        IsActive = false;
        ValidTo = validTo ?? DateTime.UtcNow;
    }
}