using System;
using System.Collections.Generic;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class BasicTenantInfo
{
    [CanBeNull] public Guid? TenantId { get; }

    [CanBeNull] public string TenantNormalized { get; }

    public bool IsSystemTenant { get; } = false;

    public List<Guid> AllowedTenantIds { get; }

    public List<Subscription> AllowedSubscriptions { get; }


    public BasicTenantInfo(
        Guid? tenantId,
        bool isSystemTenant,
        [CanBeNull] List<Guid> allowedTenantIds,
        [CanBeNull] List<Subscription> allowedSubscriptions,
        [CanBeNull] string tenantNormalized = null)
    {
        TenantId = tenantId;
        TenantNormalized = tenantNormalized;
        IsSystemTenant = isSystemTenant;
        AllowedTenantIds = allowedTenantIds ?? [];
        AllowedSubscriptions = allowedSubscriptions ?? [];
    }
}