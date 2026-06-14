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

    // public List<Guid> AllowedCustomerIds { get; }

    public List<string> AllowedScopeKeys { get; }

    public BasicTenantInfo(
        Guid? tenantId,
        bool isSystemTenant,
        [CanBeNull] List<Guid> allowedTenantIds,
        // [CanBeNull] List<Guid> allowedCustomerIds,
        [CanBeNull] List<string> allowedScopeKeys,
        [CanBeNull] string tenantNormalized = null)
    {
        TenantId = tenantId;
        TenantNormalized = tenantNormalized;
        IsSystemTenant = isSystemTenant;
        AllowedTenantIds = allowedTenantIds ?? [];
        // AllowedCustomerIds = allowedCustomerIds ?? [];
        AllowedScopeKeys = allowedScopeKeys ?? [];
    }
}