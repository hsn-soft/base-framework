using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class BasicTenantInfo
{
    [CanBeNull] public Guid? TenantId { get; }

    [CanBeNull] public string TenantNormalized { get; }

    public bool IsSystemTenant { get; } = false;

    public List<Guid> AllowedTenantIds { get; }


    public BasicTenantInfo(Guid? tenantId, bool isSystemTenant, [CanBeNull] List<Guid> allowedTenantIds, [CanBeNull] string tenantNormalized = null)
    {
        TenantId = tenantId;
        TenantNormalized = tenantNormalized;
        IsSystemTenant = isSystemTenant;
        AllowedTenantIds = allowedTenantIds ?? [];
    }
}