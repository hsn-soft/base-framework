using System;
using System.Collections.Generic;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public interface ICurrentTenant
{
    bool IsAvailable { get; }

    [CanBeNull] Guid? Id { get; }

    [CanBeNull] string Normalized { get; }

    bool IsSystemTenant { get; }

    [NotNull] List<Guid> AllowedTenantIds { get; }

    [NotNull] List<Subscription> AllowedSubscriptions { get; }

    IDisposable Change(
        Guid? id,
        bool isSystemTenant,
        [CanBeNull] List<Guid> allowedTenantIds,
        [CanBeNull] List<Subscription> allowedSubscriptions,
        [CanBeNull] string normalized = null
    );
}