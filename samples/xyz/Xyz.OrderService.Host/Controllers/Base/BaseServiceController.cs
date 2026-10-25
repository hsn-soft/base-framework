using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Xyz.OrderService.Controllers.Base;

/// <summary>
/// What every controller of the order service is: a web api controller of the area <c>order-service</c> that answers JSON and needs a valid JWT bearer token
/// (a controller that must be open says so with <c>[AllowAnonymous]</c>). Controllers derive from it and only add their route,
/// <c>api/order-service/v1/commercial/&lt;controller&gt;</c> (or <c>public</c>).
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("order-service")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase
{
    /// <summary>
    /// Translates a text into the language of the request, for a response body that carries a text (<c>L["OrderStatus:Pending"]</c>; the key itself comes back when no language has it). The texts are the merged ones of
    /// the framework, the shared project and the service. The errors need no translating here: an exception names its text and the host translates it.
    /// </summary>
    protected IStringLocalizer L => field ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();
}
