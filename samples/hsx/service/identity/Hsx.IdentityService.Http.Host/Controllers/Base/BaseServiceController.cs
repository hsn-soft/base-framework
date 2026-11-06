using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers.Base;

/// <summary>
/// What every controller of the identity service is: a web api controller of the area <c>identity-service</c> that answers JSON and needs a valid JWT bearer token. Controllers derive from it and only add their route,
/// <c>api/identity-service/v1/commercial/&lt;controller&gt;</c>, and one-line actions: each calls one method of an application service and answers a DTO (the ADR "The API contract").
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[Area("identity-service")]
[ApiController]
public abstract class BaseServiceController : ApiControllerBase;
