using System;
using AutoMapper;
using HsnSoft.Base.AspNetCore.Mvc.Services;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.AspNetCore.Mvc.UI.RazorPages;

public abstract class BasePageModel : PageModel
{
    private IStringLocalizer _localizer;
    public IServiceProvider ServiceProvider { get; protected set; }


    // protected ICurrentUser CurrentUser => LazyServiceProvider.GetRequiredService<ICurrentUser>();
    //
    // protected ICurrentTenant CurrentTenant => LazyServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IAuthorizationService AuthorizationService => ServiceProvider.GetRequiredService<IAuthorizationService>();

    protected IRazorRenderService RazorRenderService => ServiceProvider.GetRequiredService<IRazorRenderService>();
    protected IMapper Mapper => ServiceProvider.GetRequiredService<IMapper>();

    protected IBaseLogger Logger => ServiceProvider.GetRequiredService<IBaseLogger>();

    protected IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();

    protected Type LocalizationResourceType { get; set; }

    protected IStringLocalizer L
    {
        get
        {
            if (_localizer == null)
            {
                _localizer = CreateLocalizer();
            }

            return _localizer;
        }
    }

    protected virtual IStringLocalizer CreateLocalizer()
    {
        if (LocalizationResourceType != null)
        {
            return StringLocalizerFactory.Create(LocalizationResourceType);
        }

        var localizer = StringLocalizerFactory.CreateDefaultOrNull();
        if (localizer == null)
        {
            throw new BaseException($"Set {nameof(LocalizationResourceType)} or define the default localization resource type to be able to use the {nameof(L)} object!");
        }

        return localizer;
    }


    // protected virtual Task CheckPolicyAsync(string policyName)
    // {
    //     return AuthorizationService.CheckAsync(policyName);
    // }

    protected virtual PartialViewResult PartialView<TModel>(string viewName, TModel model)
    {
        return new PartialViewResult { ViewName = viewName, ViewData = new ViewDataDictionary<TModel>(ViewData, model), TempData = TempData };
    }


    protected RedirectResult RedirectSafely(string returnUrl, string returnUrlHash = null)
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

    private string NormalizeReturnUrl(string returnUrl)
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
        return "~/"; //TODO: ???
    }
}