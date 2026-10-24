using Microsoft.AspNetCore.Mvc;

namespace Abc.IdentityService.Http.Host.Controllers;

public sealed record IdentityInfo(string Service, DateTimeOffset Time);

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
    /// <summary>The first endpoint of the service: it answers who is speaking and when.</summary>
    [HttpGet("ping")]
    public IdentityInfo Ping() => new("Abc.IdentityService", DateTimeOffset.UtcNow);
}
