using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>The default <see cref="ICurrentTenant"/>: the tenant a <see cref="Change"/> scope set, else the one the first <see cref="ITenantSource"/> that knows it says.</summary>
public sealed class CurrentTenant(IEnumerable<ITenantSource>? sources = null) : ICurrentTenant
{
    private readonly ITenantSource[] _sources = [.. sources ?? []];
    private readonly AsyncLocal<TenantContext?> _scope = new();

    public bool IsAvailable => Id.HasValue;

    public Guid? Id => Resolve()?.Id;

    public bool IsSystemTenant => Resolve()?.IsSystemTenant ?? false;

    public IReadOnlyList<Guid> AllowedTenantIds
    {
        get
        {
            TenantContext? tenant = Resolve();
            IEnumerable<Guid> allowed = tenant?.AllowedTenantIds ?? [];
            return [.. (tenant?.Id is { } own ? allowed.Append(own) : allowed).Distinct()];
        }
    }

    public IDisposable Change(Guid? id, bool isSystemTenant = false, IEnumerable<Guid>? allowedTenantIds = null)
    {
        TenantContext? parent = _scope.Value;
        _scope.Value = new TenantContext(id, isSystemTenant, [.. allowedTenantIds ?? []]);
        return new Restore(() => _scope.Value = parent);
    }

    private TenantContext? Resolve()
    {
        if (_scope.Value is { } scope)
        {
            return scope;
        }

        foreach (ITenantSource source in _sources)
        {
            if (source.Current is { } tenant)
            {
                return tenant;
            }
        }

        return null;
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
