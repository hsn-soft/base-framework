using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
