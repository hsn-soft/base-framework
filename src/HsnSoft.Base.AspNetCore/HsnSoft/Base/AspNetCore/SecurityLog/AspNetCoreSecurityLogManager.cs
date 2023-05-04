using System.Threading.Tasks;
using HsnSoft.Base.AspNetCore.WebClientInfo;
using HsnSoft.Base.Clients;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.SecurityLog;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.SecurityLog;

public class AspNetCoreSecurityLogManager : DefaultSecurityLogManager
{
    public AspNetCoreSecurityLogManager(
        IOptions<BaseSecurityLogOptions> securityLogOptions,
        ISecurityLogStore securityLogStore,
        ILogger<AspNetCoreSecurityLogManager> logger,
        IClock clock,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ICurrentClient currentClient,
        IHttpContextAccessor httpContextAccessor,
        ICorrelationIdProvider correlationIdProvider,
        IWebClientInfoProvider webClientInfoProvider)
        : base(securityLogOptions, securityLogStore)
    {
        Logger = logger;
        Clock = clock;
        CurrentUser = currentUser;
        CurrentTenant = currentTenant;
        CurrentClient = currentClient;
        HttpContextAccessor = httpContextAccessor;
        CorrelationIdProvider = correlationIdProvider;
        WebClientInfoProvider = webClientInfoProvider;
    }

    protected ILogger<AspNetCoreSecurityLogManager> Logger { get; }
    protected IClock Clock { get; }
    protected ICurrentUser CurrentUser { get; }
    protected ICurrentTenant CurrentTenant { get; }
    protected ICurrentClient CurrentClient { get; }
    protected IHttpContextAccessor HttpContextAccessor { get; }
    protected ICorrelationIdProvider CorrelationIdProvider { get; }
    protected IWebClientInfoProvider WebClientInfoProvider { get; }

    protected override async Task<SecurityLogInfo> CreateAsync()
    {
        var securityLogInfo = await base.CreateAsync();

        securityLogInfo.CreationTime = Clock.Now;

        securityLogInfo.TenantId = CurrentTenant.Id;
        securityLogInfo.TenantName = CurrentTenant.Name;

        securityLogInfo.UserId = CurrentUser.Id;
        securityLogInfo.UserName = CurrentUser.UserName;

        securityLogInfo.ClientId = CurrentClient.Id;

        securityLogInfo.CorrelationId = CorrelationIdProvider.Get();

        securityLogInfo.ClientIpAddress = WebClientInfoProvider.ClientIpAddress;
        securityLogInfo.BrowserInfo = WebClientInfoProvider.BrowserInfo;

        return securityLogInfo;
    }
}