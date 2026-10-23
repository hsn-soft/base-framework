using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xyz.AuthServer.Host.Services;

namespace Xyz.AuthServer.Host.Controllers;

public sealed record LoginRequest([property: Required] string Username, [property: Required] string Password);

public sealed record RefreshRequest([property: Required] string RefreshToken);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(TokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        UserAccount account = tokens.ValidateCredentials(request.Username, request.Password)
                              ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials");

        return Ok(ToResponse(await tokens.IssueAsync(account, cancellationToken)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        TokenPair? pair = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        return pair is null
            ? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token")
            : Ok(ToResponse(pair));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = BasePolicies.Authenticated)]
    public IActionResult Me() => Ok(new { Subject = User.GetUserId(), Roles = User.GetRoles() });

    private static TokenResponse ToResponse(TokenPair pair) => new(pair.AccessToken, "Bearer", pair.ExpiresInSeconds, pair.RefreshToken);
}
