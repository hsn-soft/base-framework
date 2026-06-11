using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public interface ICurrentTenant
{
    bool IsAvailable { get; }

    [CanBeNull] Guid? Id { get; }

    [CanBeNull] string Normalized { get; }

    bool IsSystemTenant { get; }

    [NotNull] List<Guid> AllowedTenantIds { get; }

    // [NotNull] List<Guid> AllowedCustomerIds { get; }

    [NotNull] List<string> AllowedScopeKeys { get; }

    IDisposable Change(
        Guid? id,
        bool isSystemTenant,
        [CanBeNull] List<Guid> allowedTenantIds,
        // [CanBeNull] List<Guid> allowedCustomerIds,
        [CanBeNull] List<string> allowedScopeKeys,
        [CanBeNull] string normalized = null
    );
}