using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Abc.IdentityService.Http.Host.Controllers.Base;

/// <summary>
/// What every controller of the identity service is: a web api controller of the area <c>identity-service</c> that answers JSON and needs a valid JWT bearer token
/// (a controller that must be open says so with <c>[AllowAnonymous]</c>).
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("identity-service")]
[ApiController]
public abstract class BaseServiceController : ControllerBase
{
    /// <summary>
    /// Writes the application logs of a controller (<c>Logger.LogInformation("User {CreatedUserId} created", id)</c>): they are the <c>AppLog</c>s of the service, under the category of the
    /// controller that writes them, with the <c>CorrelationId</c> of the request. Name the values of a message after what they are: <c>UserId</c> and <c>UserRoles</c> are the
    /// fields of who made the request. Nothing to inject: a controller only uses it.
    /// </summary>
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
