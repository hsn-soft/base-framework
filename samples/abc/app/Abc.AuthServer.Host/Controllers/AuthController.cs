using System.ComponentModel.DataAnnotations;
using Abc.AuthServer.Host.Controllers.Base;
using Abc.AuthServer.Host.Options;
using Abc.AuthServer.Host.Services;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abc.AuthServer.Host.Controllers;

public sealed class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

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
public sealed class AuthController(TokenService tokens) : BaseServiceController
{
    /// <summary>The one open endpoint: a user name and a password for an access token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public TokenDto Login([FromBody] LoginDto input)
    {
        UserAccount account = tokens.ValidateCredentials(input.Username, input.Password)
                              ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials");

        AccessToken token = tokens.Issue(account);
        Logger.LogInformation("{Account} signed in", account.Username);
        return new TokenDto { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds };
    }

    /// <summary>Who the token says the caller is.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public MeDto Me() => new() { Subject = User.GetUserId(), Roles = [.. User.GetRoles()] };
}
