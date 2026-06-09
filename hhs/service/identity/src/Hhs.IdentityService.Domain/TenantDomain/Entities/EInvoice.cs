using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class EInvoice : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; private set; }

    public ClientNew Client { get; private set; } = null!;

    public Guid ProductTypeId { get; private set; }

    public ProductType ProductType { get; private set; } = null!;

    public string InvoiceNo { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public DateTime InvoiceDate { get; private set; }
}