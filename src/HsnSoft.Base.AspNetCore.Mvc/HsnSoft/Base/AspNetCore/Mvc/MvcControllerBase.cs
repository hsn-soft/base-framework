using System;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Guids;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.AspNetCore.Mvc;

public abstract class BaseController : Controller
{
    public IBaseLazyServiceProvider LazyServiceProvider { get; set; }

    protected IGuidGenerator GuidGenerator => LazyServiceProvider.LazyGetService<IGuidGenerator>(SimpleGuidGenerator.Instance);

    protected ILoggerFactory LoggerFactory => LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance);

    protected IAuthorizationService AuthorizationService => LazyServiceProvider.LazyGetRequiredService<IAuthorizationService>();

    protected IStringLocalizerFactory StringLocalizerFactory => LazyServiceProvider.LazyGetRequiredService<IStringLocalizerFactory>();

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