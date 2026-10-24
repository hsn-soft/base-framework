using Microsoft.AspNetCore.Mvc;

namespace Abc.IdentityService.Http.Host.Controllers.Base;

/// <summary>What every controller of the identity service is: a web api controller that answers JSON.</summary>
[Produces("application/json")]
[ApiController]
public abstract class BaseServiceController : ControllerBase
{
}
