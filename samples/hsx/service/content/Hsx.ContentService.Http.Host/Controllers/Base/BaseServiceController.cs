using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hsx.ContentService.Controllers.Base;

/// <summary>
/// What every controller of the content service is: a web api controller of the area <c>content-service</c> that answers JSON and needs a valid JWT bearer token (a controller that must be open says so with <c>[AllowAnonymous]</c>).
/// Controllers derive from it and only add their route, <c>api/content-service/v1/commercial/&lt;controller&gt;</c>.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("content-service")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase
{
    /// <summary>Translates a text into the language of the request, for a response body that carries a text.</summary>
    protected IStringLocalizer L => field ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();
}
