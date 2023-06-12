using System.Collections.Generic;
using System.Globalization;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.AspNetCore.Security;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.SecurityLog;
using HsnSoft.Base.AspNetCore.Threading;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.AspNetCore.WebClientInfo;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.SecurityLog;
using HsnSoft.Base.Threading;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

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

    public static IServiceCollection AddBaseAspNetCoreJsonLocalization(this IServiceCollection services)
    {
        services.AddTransient<BaseLocalizationMiddleware>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = new List<CultureInfo> { new("en"), new("ru"), new("tr") };
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        services.AddMemoryCache();
        services.AddLocalization();
        services.AddTransient<IStringLocalizer, CacheStringLocalizer>();
        services.AddSingleton<IStringLocalizerFactory, CacheStringLocalizerFactory>();

        return services;
    }
}