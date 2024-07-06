using System;
using HsnSoft.Base.Guids;
using HsnSoft.Base.Logging;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.AspNetCore.Mvc;

public abstract class BaseController : Controller
{
    protected IServiceProvider ServiceProvider { get; set; }

    protected IGuidGenerator GuidGenerator => SimpleGuidGenerator.Instance;

    protected IBaseLogger Logger => ServiceProvider.GetRequiredService<IBaseLogger>();

    protected IAuthorizationService AuthorizationService => ServiceProvider.GetRequiredService<IAuthorizationService>();

    protected ICurrentUser CurrentUser => ServiceProvider.GetRequiredService<ICurrentUser>();

    protected ICurrentTenant CurrentTenant => ServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();

    protected virtual RedirectResult RedirectSafely(string returnUrl, string returnUrlHash = null)
    {
        return Redirect(GetRedirectUrl(returnUrl, returnUrlHash));
    }

    protected virtual string GetRedirectUrl(string returnUrl, string returnUrlHash = null)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (!returnUrlHash.IsNullOrWhiteSpace())
        {
            returnUrl = returnUrl + returnUrlHash;
        }

        return returnUrl;
    }

    protected virtual string NormalizeReturnUrl(string returnUrl)
    {
        if (returnUrl.IsNullOrEmpty())
        {
            return GetAppHomeUrl();
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return GetAppHomeUrl();
    }

    protected virtual string GetAppHomeUrl()
    {
        return Url.Content("~/");
    }
}