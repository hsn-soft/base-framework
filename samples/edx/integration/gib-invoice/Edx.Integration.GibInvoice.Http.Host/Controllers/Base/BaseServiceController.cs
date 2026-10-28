using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Integration.GibInvoice.Http.Host.Controllers.Base;

/// <summary>
/// What every controller of the integration service is: a web api controller of the area <c>integration-service</c> that answers JSON and needs a valid JWT bearer token
/// (a controller that must be open says so with <c>[AllowAnonymous]</c>). Controllers derive from it and only add their route,
/// <c>api/integration-service/v1/commercial/&lt;controller&gt;</c>.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("integration-service")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase;
