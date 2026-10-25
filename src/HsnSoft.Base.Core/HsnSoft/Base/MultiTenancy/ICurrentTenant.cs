using System;
using System.Collections.Generic;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// The tenant of the request: what the token says (<c>tenant_id</c>, <c>is_system_tenant</c>, <c>allowed_tenant_id</c>) unless code changed it with <see cref="Change"/>
/// (a seed, a worker). The system tenant sees every tenant; anyone else sees the tenants in <see cref="AllowedTenantIds"/>, which always has its own.
/// </summary>
public interface ICurrentTenant
{
    bool IsAvailable { get; }

    Guid? Id { get; }

    bool IsSystemTenant { get; }

    /// <summary>The tenants whose entities may be seen: the tenant itself and the ones the token allows.</summary>
    IReadOnlyList<Guid> AllowedTenantIds { get; }

    /// <summary>Acts as another tenant until the returned scope is disposed (it works across <c>await</c>; the previous tenant comes back).</summary>
    IDisposable Change(Guid? id, bool isSystemTenant = false, IEnumerable<Guid>? allowedTenantIds = null);
}
