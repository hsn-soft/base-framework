using System;
using System.Collections.Generic;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>Who the tenant of the caller is, as a <see cref="ITenantSource"/> knows it.</summary>
/// <param name="Id">The tenant of the caller.</param>
/// <param name="IsSystemTenant">Whether the caller belongs to the system tenant, which sees every tenant.</param>
/// <param name="AllowedTenantIds">The other tenants the caller may see (its own is always allowed).</param>
public sealed record TenantContext(Guid? Id, bool IsSystemTenant = false, IReadOnlyList<Guid>? AllowedTenantIds = null);
