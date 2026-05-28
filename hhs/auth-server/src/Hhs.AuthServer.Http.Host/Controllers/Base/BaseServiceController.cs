using Hhs.AuthServer.Localization;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base.AspNetCore.Mvc;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Controllers.Base;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("auth-server")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase
{
    [NotNull]
    protected IStringLocalizer L { get; }

    protected BaseServiceController(IServiceProvider provider)
    {
        ServiceProvider = provider ?? throw new ArgumentNullException(nameof(provider), "BaseServiceController IServiceProvider is null");

        L = StringLocalizerFactory.CreateMultiple(new List<Type>
        {
            typeof(AuthServerResource),
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });
    }
}