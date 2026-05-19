using System;
using System.Collections.Generic;
using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class CurrentTenant : ICurrentTenant, ITransientDependency
{
    private readonly ICurrentTenantAccessor _currentTenantAccessor;

    public CurrentTenant(ICurrentTenantAccessor currentTenantAccessor)
    {
        _currentTenantAccessor = currentTenantAccessor;
    }

    public virtual bool IsAvailable => Id.HasValue;


    public virtual Guid? Id => _currentTenantAccessor.Current?.TenantId;
    public virtual string Normalized => _currentTenantAccessor.Current?.TenantNormalized;
    public virtual bool IsSystemTenant => _currentTenantAccessor.Current?.IsSystemTenant ?? false;
    public virtual List<Guid> AllowedTenantIds => _currentTenantAccessor.Current?.AllowedTenantIds ?? [];


    public IDisposable Change(Guid? id, bool isSystemTenant, List<Guid> allowedTenantIds, string normalized = null)
    {
        return SetCurrent(id, isSystemTenant, allowedTenantIds, normalized);
    }

    private IDisposable SetCurrent(Guid? tenantId, bool isSystemTenant, [CanBeNull] List<Guid> allowedTenantIds, string normalized = null)
    {
        var parentScope = _currentTenantAccessor.Current;
        _currentTenantAccessor.Current = new BasicTenantInfo(tenantId, isSystemTenant, allowedTenantIds, normalized);
        return new DisposeAction(() => { _currentTenantAccessor.Current = parentScope; });
    }
}