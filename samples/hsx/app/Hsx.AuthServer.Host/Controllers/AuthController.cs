using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security;
using Hsx.AuthServer.Host.Controllers.Base;
using Hsx.AuthServer.Host.Security;
using Hsx.AuthServer.Host.Services;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hsx.AuthServer.Host.Controllers;

public sealed class LoginDto
{
    /// <summary>The tenant the user belongs to (its name): an email or a user name is unique only inside a tenant.</summary>
    [Required]
    public string Tenant { get; set; } = string.Empty;

    /// <summary>The email or the user name of the user.</summary>
    [Required]
    public string Account { get; set; } = string.Empty;

    [Required]
    [SensitiveData]
    public string Password { get; set; } = string.Empty;

    /// <summary>Asks for a refresh token too (kept in an HttpOnly cookie), to get a new access token without the password.</summary>
    public bool OfflineAccess { get; set; }
}

public sealed class TokenDto
{
    [SensitiveData]
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public int ExpiresIn { get; init; }
}

public sealed class MeDto
{
    public string? Subject { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];
}

[Route("api/auth-server/v1/commercial/auth")]
public sealed class AuthController(UserAuthenticator authenticator, TokenService tokens, IAuthRefreshTokenRepository refreshTokens, IOptions<AuthServerOptions> options) : BaseServiceController
{
    private readonly AuthServerOptions _options = options.Value;

    /// <summary>A tenant, an account (an email or a user name) and a password (of a user of the identity service) for an access token; with <c>OfflineAccess</c> a refresh token comes too, in a cookie.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken = default)
    {
        SignInOrigin origin = new(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        AuthenticatedUser user = await authenticator.AuthenticateAsync(input.Tenant, input.Account, input.Password, origin, cancellationToken)
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

        if (input.OfflineAccess)
        {
            SetRefreshCookie(await refreshTokens.IssueAsync(user.Id, user.TenantId, Lifetime, cancellationToken));
        }

        Logger.LogInformation("{Account} signed in", user.Email);
        return ToDto(tokens.Issue(user));
    }

    /// <summary>A new access token for the refresh token of the cookie; the token is used once and the cookie gets the one that takes its place.</summary>
    [AllowAnonymous]
    [HttpPost("token-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> RefreshAsync(CancellationToken cancellationToken = default)
    {
        string? token = Request.Cookies[_options.RefreshCookieName];
        (Guid UserId, string NewToken)? rotated = string.IsNullOrEmpty(token) ? null : await refreshTokens.RotateAsync(token, Lifetime, cancellationToken);
        AuthenticatedUser? user = rotated is { } found ? await authenticator.FindForRefreshAsync(found.UserId, cancellationToken) : null;
        if (rotated is null || user is null)
        {
            Response.Cookies.Delete(_options.RefreshCookieName, CookieOptions());
            throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid refresh token").WithKey("invalid_refresh_token");
        }

        SetRefreshCookie(rotated.Value.NewToken);
        return ToDto(tokens.Issue(user));
    }

    /// <summary>Ends the refresh token of the cookie and removes the cookie. The access token lives until it ends.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        string? token = Request.Cookies[_options.RefreshCookieName];
        if (!string.IsNullOrEmpty(token))
        {
            await refreshTokens.RevokeAsync(token, cancellationToken);
        }

        Response.Cookies.Delete(_options.RefreshCookieName, CookieOptions());
        return NoContent();
    }

    /// <summary>Who the token says the caller is.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public MeDto Me() => new() { Subject = User.GetUserId(), Roles = [.. User.GetRoles()] };

    private TimeSpan Lifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    private static TokenDto ToDto(AccessToken token) => new() { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds };

    private CookieOptions CookieOptions() => new() { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict, Path = _options.RefreshCookiePath };

    private void SetRefreshCookie(string token)
    {
        CookieOptions cookie = CookieOptions();
        cookie.Expires = DateTimeOffset.UtcNow.Add(Lifetime);
        Response.Cookies.Append(_options.RefreshCookieName, token, cookie);
    }
}
