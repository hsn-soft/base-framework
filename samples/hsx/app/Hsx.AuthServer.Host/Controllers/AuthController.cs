using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security;
using Hsx.AuthServer.Host.Controllers.Base;
using Hsx.AuthServer.Host.Security;
using Hsx.AuthServer.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.AuthServer.Host.Controllers;

public sealed class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [SensitiveData]
    public string Password { get; set; } = string.Empty;
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
public sealed class AuthController(UserAuthenticator authenticator, TokenService tokens) : BaseServiceController
{
    /// <summary>The one open endpoint: an email and a password (of a user of the identity service) for an access token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser user = await authenticator.AuthenticateAsync(input.Email, input.Password, cancellationToken)
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

        AccessToken token = tokens.Issue(user);
        Logger.LogInformation("{Account} signed in", user.Email);
        return new TokenDto { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds };
    }

    /// <summary>Who the token says the caller is.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public MeDto Me() => new() { Subject = User.GetUserId(), Roles = [.. User.GetRoles()] };
}
