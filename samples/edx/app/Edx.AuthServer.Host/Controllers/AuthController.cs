using System.ComponentModel.DataAnnotations;
using Edx.AuthServer.Host.Controllers.Base;
using Edx.AuthServer.Host.Security;
using Edx.AuthServer.Host.Services;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.AuthServer.Host.Controllers;

public sealed class LoginDto
{
    /// <summary>The code of the tenant the user belongs to (<c>system-tenant</c>, <c>veriban</c>, <c>bilgera</c>): a user name is only unique in a tenant.</summary>
    [Required]
    public string Tenant { get; set; } = string.Empty;

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

    public Guid? TenantId { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];
}

[Route("api/auth-server/v1/commercial/auth")]
public sealed class AuthController(UserAuthenticator authenticator, TokenService tokens) : BaseServiceController
{
    /// <summary>The one open endpoint: the tenant, a user name and a password for an access token, which every other host of the system accepts.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser user = await authenticator.AuthenticateAsync(input.Tenant, input.Username, input.Password, cancellationToken)
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

        AccessToken token = tokens.Issue(user);
        Logger.LogInformation("{Account} of {TenantCode} signed in", user.UserName, input.Tenant);
        return new TokenDto { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds };
    }

    /// <summary>Who the token says the caller is.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public MeDto Me() => new()
    {
        Subject = User.GetUserId(),
        TenantId = Guid.TryParse(User.FindFirst("tenant_id")?.Value, out Guid tenantId) ? tenantId : null,
        Roles = [.. User.GetRoles()],
    };
}
