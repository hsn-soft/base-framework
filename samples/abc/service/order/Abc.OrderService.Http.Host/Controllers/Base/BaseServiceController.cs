using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Abc.OrderService.Http.Host.Controllers.Base;

/// <summary>
/// What every controller of the order service is: a web api controller of the area <c>order-service</c> that answers JSON and needs a valid JWT bearer token
/// (a controller that must be open says so with <c>[AllowAnonymous]</c>). Controllers derive from it and only add their route,
/// <c>api/order-service/v1/commercial/&lt;controller&gt;</c> (or <c>public</c>).
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("order-service")]
[ApiController]
public abstract class BaseServiceController : ControllerBase
{
    /// <summary>Writes the application logs of a controller: they are the <c>AppLog</c>s of the service, under the category of the controller that writes them.</summary>
    protected ILogger Logger => field ??= HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    /// <summary>
    /// Translates a text into the language of the request, for a response body that carries a text (<c>L["OrderStatus:Pending"]</c>; the key itself comes back when no language has it). The texts are the merged ones of
    /// the framework, the shared project and the service. The errors need no translating here: an exception names its text and the host translates it.
    /// </summary>
    protected IStringLocalizer L => field ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();

    /// <summary>Who is making the request (the token): its id, user name and tenant.</summary>
    protected ICurrentUser CurrentUser => field ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>The tenant of the request, and the ones it may see.</summary>
    protected ICurrentTenant CurrentTenant => field ??= HttpContext.RequestServices.GetRequiredService<ICurrentTenant>();
}
