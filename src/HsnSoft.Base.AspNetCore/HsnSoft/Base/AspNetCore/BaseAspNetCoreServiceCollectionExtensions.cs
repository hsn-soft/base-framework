using System.Collections.Generic;
using System.Globalization;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.AspNetCore.Security;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.WebClientInfo;
using HsnSoft.Base.Clients;
using HsnSoft.Base.Localization;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.AspNetCore;

public static class BaseAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddBaseAspNetCoreContextCollection(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddHttpContextAccessor();
        services.AddTransient<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
        services.AddTransient<ICurrentClient, CurrentClient>();
        services.AddTransient<ICurrentUser, CurrentUser>();

        services.AddTransient<ICurrentTenantAccessor>(sp=> new BasicCurrentTenantAccessor(sp));

        services.AddTransient<ICurrentTenant, CurrentTenant>();

        services.AddTransient<IWebClientInfoProvider, HttpContextWebClientInfoProvider>();
        services.AddScoped<BaseClaimsMapMiddleware>();
        services.AddScoped<BaseSecurityHeadersMiddleware>();

        return services;
    }

    public static IServiceCollection AddBaseAspNetCoreJsonLocalization(this IServiceCollection services)
    {
        services.AddScoped<BaseLocalizationMiddleware>();

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