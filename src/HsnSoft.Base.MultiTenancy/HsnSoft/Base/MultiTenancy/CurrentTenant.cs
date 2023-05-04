using System;
using HsnSoft.Base.DependencyInjection;

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

    public string Name => _currentTenantAccessor.Current?.Name;

    public IDisposable Change(Guid? id, string name = null)
    {
        return SetCurrent(id, name);
    }

    private IDisposable SetCurrent(Guid? tenantId, string name = null)
    {
        var parentScope = _currentTenantAccessor.Current;
        _currentTenantAccessor.Current = new BasicTenantInfo(tenantId, name);
        return new DisposeAction(() =>
        {
            _currentTenantAccessor.Current = parentScope;
        });
    }
}