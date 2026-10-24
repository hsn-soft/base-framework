using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abc.ShipmentService.Http.Host.Controllers.Base;

/// <summary>
/// What every controller of the shipment service is: a web api controller of the area <c>shipment-service</c> that answers JSON and needs a valid JWT bearer token
/// (a controller that must be open says so with <c>[AllowAnonymous]</c>). Controllers derive from it and only add their route,
/// <c>api/shipment-service/v1/commercial/&lt;controller&gt;</c> (or <c>public</c>).
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("shipment-service")]
[ApiController]
public abstract class BaseServiceController : ControllerBase
{
    /// <summary>Writes the application logs of a controller: they are the <c>AppLog</c>s of the service, under the category of the controller that writes them.</summary>
    protected ILogger Logger => field ??= HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
}
