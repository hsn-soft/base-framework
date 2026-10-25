using System;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>An entity that belongs to a tenant. The persistence gives a new one the current tenant (<see cref="AssignTenant"/>) and shows a caller only the tenants it may see.</summary>
public interface IMultiTenant
{
    Guid TenantId { get; }

    /// <summary>Sets the tenant of an entity that has none yet. Called by the persistence when the entity is added.</summary>
    void AssignTenant(Guid tenantId);
}
