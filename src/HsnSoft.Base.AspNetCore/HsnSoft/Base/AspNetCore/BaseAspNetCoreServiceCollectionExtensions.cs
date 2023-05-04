using HsnSoft.Base.AspNetCore.Security;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.SecurityLog;
using HsnSoft.Base.AspNetCore.Threading;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.AspNetCore.WebClientInfo;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.SecurityLog;
using HsnSoft.Base.Threading;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore;

public static class BaseAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAspNetCoreServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<BaseClaimsMapMiddleware>();
        services.AddTransient<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
        services.AddTransient<BaseSecurityHeadersMiddleware>();
        services.AddTransient<ISecurityLogManager, AspNetCoreSecurityLogManager>();

        services.AddTransient<ICancellationTokenProvider, HttpContextCancellationTokenProvider>();

        services.AddTransient<BaseCorrelationIdMiddleware>();
        services.AddTransient<ICorrelationIdProvider, AspNetCoreCorrelationIdProvider>();

        services.AddTransient<IWebClientInfoProvider, HttpContextWebClientInfoProvider>();

        return services;
    }
}