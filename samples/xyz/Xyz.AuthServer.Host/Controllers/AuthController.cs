using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xyz.AuthServer.Controllers.Base;
using Xyz.AuthServer.Domain.UserHistoryDomain.Consts;
using Xyz.AuthServer.Domain.UserHistoryDomain.Entities;
using Xyz.AuthServer.Security;
using Xyz.AuthServer.Services;

namespace Xyz.AuthServer.Controllers;

public sealed class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [SensitiveData]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshDto
{
    [Required]
    [SensitiveData]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class TokenDto
{
    [SensitiveData]
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public int ExpiresIn { get; init; }

    [SensitiveData]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class MeDto
{
    public string? Subject { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];
}

[Route("api/auth-server/v1/commercial/auth")]
public sealed class AuthController(UserAuthenticator authenticator, TokenService tokens, IRepository<UserHistory, Guid> histories, ICurrentTenant tenant) : BaseServiceController
{
    /// <summary>A user name and a password for an access token and a refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken = default)
    {
        AuthenticatedUser user = await authenticator.AuthenticateAsync(input.Username, input.Password, cancellationToken)
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

        return await IssueAsync(user, UserHistoryConsts.Login, cancellationToken);
    }

    /// <summary>A refresh token for a new pair; the refresh token that was sent stops working.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TokenDto> RefreshAsync([FromBody] RefreshDto input, CancellationToken cancellationToken = default)
    {
        Guid? userId = await tokens.ConsumeRefreshTokenAsync(input.RefreshToken, cancellationToken);
        AuthenticatedUser user = (userId is null ? null : await authenticator.FindActiveAsync(userId.Value, cancellationToken))
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token").WithKey("invalid_refresh_token");

        return await IssueAsync(user, UserHistoryConsts.Refresh, cancellationToken);
    }

    /// <summary>Ends a session: the refresh token stops working.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<NoContentResult> LogoutAsync([FromBody] RefreshDto input, CancellationToken cancellationToken = default)
    {
        await tokens.RevokeAsync(input.RefreshToken, cancellationToken);
        return NoContent();
    }

    /// <summary>Who the token says the caller is.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public MeDto Me() => new() { Subject = User.GetUserId(), Roles = [.. User.GetRoles()] };

    private async Task<TokenDto> IssueAsync(AuthenticatedUser user, string action, CancellationToken cancellationToken)
    {
        TokenPair pair = await tokens.IssueAsync(user, cancellationToken);

        // every token that is issued is written to the history (its own database); nobody is signed in yet, so the record is written as the user who signs in (the audit fields and the tenant are theirs)
        using (tenant.Change(user.TenantId, user.IsSystemTenant))
        using (ActingUser.Use(new UserContext(user.Id, user.UserName, user.Roles)))
        {
            await histories.AddAsync(UserHistory.Record(user.Id, user.UserName, action), cancellationToken);
        }

        Logger.LogInformation("{Account} was issued a token ({Action})", user.UserName, action);
        return new TokenDto { AccessToken = pair.AccessToken, ExpiresIn = pair.ExpiresInSeconds, RefreshToken = pair.RefreshToken };
    }
}
