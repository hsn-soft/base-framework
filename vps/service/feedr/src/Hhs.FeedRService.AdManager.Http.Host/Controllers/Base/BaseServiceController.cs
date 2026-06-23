using Hhs.Shared.Localization;
using HsnSoft.Base.AspNetCore.Mvc;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Hhs.FeedRService.Domain.Localization;

namespace Hhs.FeedRService.AdManager.Controllers.Base;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("feedr-admanager")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase
{
    [NotNull]
    protected IStringLocalizer L { get; }

    protected BaseServiceController(IServiceProvider provider)
    {
        ServiceProvider = provider ?? throw new ArgumentNullException(nameof(provider), "BaseServiceController IServiceProvider is null");

        L = StringLocalizerFactory.CreateMultiple([
            typeof(FeedRServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]);
    }
}