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

    public string Domain => _currentTenantAccessor.Current?.Domain;

    public IDisposable Change(Guid? id, string name = null, string domain = null)
    {
        return SetCurrent(id, name, domain);
    }

    private IDisposable SetCurrent(Guid? tenantId, string name = null, string domain = null)
    {
        var parentScope = _currentTenantAccessor.Current;
        _currentTenantAccessor.Current = new BasicTenantInfo(tenantId, name, domain);
        return new DisposeAction(() =>
        {
            _currentTenantAccessor.Current = parentScope;
        });
    }
}