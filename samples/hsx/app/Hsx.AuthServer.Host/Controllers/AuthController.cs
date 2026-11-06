using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Services;
using Hsx.AuthServer.Host.Controllers.Base;
using Hsx.AuthServer.Host.Security.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hsx.AuthServer.Host.Controllers;

[Route("api/auth-server/v1/commercial/auth")]
public sealed class AuthController(IAuthAppService auth) : BaseServiceController
{
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SignIn)]
    [HttpPost("login")]
    public Task<TokenDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken = default)
        => auth.LoginAsync(input, cancellationToken);

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [HttpPost("token-refresh")]
    public Task<TokenDto> RefreshAsync([FromBody] RefreshDto input, CancellationToken cancellationToken = default)
        => auth.RefreshAsync(input, cancellationToken);

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SignIn)]
    [HttpPost("token")]
    public Task<TokenDto> ClientTokenAsync([FromBody] ClientTokenRequestDto input, CancellationToken cancellationToken = default)
        => auth.ClientTokenAsync(input, cancellationToken);

    [AllowAnonymous]
    [HttpPost("logout")]
    public Task<LogoutResultDto> LogoutAsync([FromBody] LogoutDto? input = null, CancellationToken cancellationToken = default)
        => auth.LogoutAsync(input, cancellationToken);

    [HttpGet("me")]
    public Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default)
        => auth.GetMeAsync(cancellationToken);
}
