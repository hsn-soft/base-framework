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

    IDisposable Change(Guid? id, bool isSystemTenant, [CanBeNull] List<Guid> allowedTenantIds, [CanBeNull] string normalized = null);
}