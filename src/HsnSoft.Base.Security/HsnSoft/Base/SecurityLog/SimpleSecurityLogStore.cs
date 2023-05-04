using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.SecurityLog;

public class SimpleSecurityLogStore : ISecurityLogStore, ITransientDependency
{
    public SimpleSecurityLogStore(ILogger<SimpleSecurityLogStore> logger, IOptions<BaseSecurityLogOptions> securityLogOptions)
    {
        Logger = logger;
        SecurityLogOptions = securityLogOptions.Value;
    }

    public ILogger<SimpleSecurityLogStore> Logger { get; set; }
    protected BaseSecurityLogOptions SecurityLogOptions { get; }

    public Task SaveAsync(SecurityLogInfo securityLogInfo)
    {
        if (!SecurityLogOptions.IsEnabled)
        {
            return Task.CompletedTask;
        }

        Logger.LogInformation(securityLogInfo.ToString());
        return Task.CompletedTask;
    }
}