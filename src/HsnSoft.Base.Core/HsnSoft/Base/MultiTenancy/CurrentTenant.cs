using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HsnSoft.Base.Security;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>The default <see cref="ICurrentTenant"/>: the tenant of <see cref="ICurrentUser"/>, or the one a <see cref="Change"/> scope set.</summary>
public sealed class CurrentTenant(ICurrentUser? currentUser = null) : ICurrentTenant
{
    private readonly AsyncLocal<Scope?> _scope = new();

    public bool IsAvailable => Id.HasValue;

    public Guid? Id => _scope.Value is { } scope ? scope.Id : currentUser?.TenantId;

    public bool IsSystemTenant => _scope.Value is { } scope ? scope.IsSystemTenant : currentUser?.IsSystemTenant ?? false;

    public IReadOnlyList<Guid> AllowedTenantIds
    {
        get
        {
            IEnumerable<Guid> allowed = _scope.Value is { } scope ? scope.Allowed : currentUser?.AllowedTenantIds ?? [];
            return [.. (Id is { } own ? allowed.Append(own) : allowed).Distinct()];
        }
    }

    public IDisposable Change(Guid? id, bool isSystemTenant = false, IEnumerable<Guid>? allowedTenantIds = null)
    {
        Scope? parent = _scope.Value;
        _scope.Value = new Scope(id, isSystemTenant, [.. allowedTenantIds ?? []]);
        return new Restore(() => _scope.Value = parent);
    }

    private sealed record Scope(Guid? Id, bool IsSystemTenant, Guid[] Allowed);

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
