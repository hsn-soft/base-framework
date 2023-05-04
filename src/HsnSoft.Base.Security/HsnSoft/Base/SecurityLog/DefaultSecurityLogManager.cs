using System;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.SecurityLog;

public class DefaultSecurityLogManager : ISecurityLogManager, ITransientDependency
{
    public DefaultSecurityLogManager(
        IOptions<BaseSecurityLogOptions> securityLogOptions,
        ISecurityLogStore securityLogStore)
    {
        SecurityLogStore = securityLogStore;
        SecurityLogOptions = securityLogOptions.Value;
    }

    protected BaseSecurityLogOptions SecurityLogOptions { get; }

    protected ISecurityLogStore SecurityLogStore { get; }

    public async Task SaveAsync(Action<SecurityLogInfo> saveAction = null)
    {
        if (!SecurityLogOptions.IsEnabled)
        {
            return;
        }

        var securityLogInfo = await CreateAsync();
        saveAction?.Invoke(securityLogInfo);
        await SecurityLogStore.SaveAsync(securityLogInfo);
    }

    protected virtual Task<SecurityLogInfo> CreateAsync()
    {
        return Task.FromResult(new SecurityLogInfo { ApplicationName = SecurityLogOptions.ApplicationName });
    }
}