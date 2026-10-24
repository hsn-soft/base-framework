using Microsoft.AspNetCore.Mvc;

namespace Abc.IdentityService.Http.Host.Controllers.Base;

/// <summary>What every controller of the identity service is: a web api controller that answers JSON.</summary>
[Produces("application/json")]
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
