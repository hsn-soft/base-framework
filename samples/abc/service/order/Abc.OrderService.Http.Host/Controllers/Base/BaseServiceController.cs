using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
