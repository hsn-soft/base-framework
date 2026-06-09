using System;
using System.Threading;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public class BasicCurrentTenantAccessor : ICurrentTenantAccessor
{
    public BasicTenantInfo Current
    {
        get => _currentScope.Value;
        set => _currentScope.Value = value;
    }

    private readonly AsyncLocal<BasicTenantInfo> _currentScope;

    public BasicCurrentTenantAccessor(IServiceProvider provider)
    {
        var currentUser = provider.GetRequiredService<ICurrentUser>();
        _currentScope = new AsyncLocal<BasicTenantInfo>
        {
            Value = new BasicTenantInfo(
                tenantId: currentUser?.TenantId,
                isSystemTenant: currentUser?.IsSystemTenant ?? false,
                allowedTenantIds: currentUser?.AllowedTenantIds ?? [],
                allowedSubscriptions: currentUser?.AllowedSubscriptions ?? [],
                tenantNormalized: currentUser?.TenantNormalized
            )
        };
    }
}