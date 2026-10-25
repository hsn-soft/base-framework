using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// What every controller of a service has at hand: <see cref="Logger"/>, <see cref="CurrentUser"/> and <see cref="CurrentTenant"/>, found from the services of the request the first time they are used, so a controller has no
/// constructor for them and no <c>IServiceProvider</c> to pass. A service derives its own base controller from this one and adds what is its own (the area, the authorization, a localizer): the attributes are the service's,
/// not the framework's. Anything else a controller needs is injected.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Writes the application logs of a controller (<c>Logger.LogInformation("User {CreatedUserId} created", id)</c>): they are the <c>AppLog</c>s of the service, under the category of the controller that writes them, with the
    /// <c>CorrelationId</c> of the request. Name the values of a message after what they are: <c>UserId</c> and <c>UserRoles</c> are the fields of who made the request.
    /// </summary>
    protected ILogger Logger => field ??= HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    /// <summary>Who is making the request (the token): its id, user name and tenant. Registered by <c>AddBaseJwtAuthentication</c>.</summary>
    protected ICurrentUser CurrentUser => field ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>The tenant of the request, and the ones it may see. Registered by the database providers (<c>AddBaseEntityConcepts</c>).</summary>
    protected ICurrentTenant CurrentTenant => field ??= HttpContext.RequestServices.GetRequiredService<ICurrentTenant>();
}
