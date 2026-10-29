using System;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// An entity that belongs to a tenant. The persistence gives a new one the current tenant when it has none (it writes the property itself, so a private setter is enough) and shows a caller only the
/// tenants it may see.
/// </summary>
public interface IMultiTenant
{
    Guid TenantId { get; }
}
