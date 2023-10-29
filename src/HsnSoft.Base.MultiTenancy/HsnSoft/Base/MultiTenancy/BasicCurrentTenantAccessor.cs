using System;
using System.Threading;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public class BasicCurrentTenantAccessor : ICurrentTenantAccessor
{
    public BasicTenantInfo Current {
        get => _currentScope.Value;
        set => _currentScope.Value = value;
    }

    private readonly AsyncLocal<BasicTenantInfo> _currentScope;
    internal BasicCurrentTenantAccessor(IServiceProvider provider)
    {
        var currentUser = provider.GetRequiredService<ICurrentUser>();
        _currentScope = new AsyncLocal<BasicTenantInfo>
        {
            Value = new BasicTenantInfo(currentUser?.TenantId)
        };
    }
}