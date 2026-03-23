using System;
using HsnSoft.Base.Logging;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.AspNetCore.Mvc;

public abstract class ApiControllerBase : ControllerBase
{
    protected IServiceProvider ServiceProvider { get; set; }

    protected IBaseLogger Logger => ServiceProvider.GetRequiredService<IBaseLogger>();

    protected IAuthorizationService AuthorizationService => ServiceProvider.GetRequiredService<IAuthorizationService>();
    protected ICurrentUser CurrentUser => ServiceProvider.GetRequiredService<ICurrentUser>();

    protected ICurrentTenant CurrentTenant => ServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
}