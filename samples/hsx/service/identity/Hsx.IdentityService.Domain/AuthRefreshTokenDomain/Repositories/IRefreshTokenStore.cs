namespace Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;

/// <summary>What the refresh tokens of the auth server are kept with. Only the hash of a token is stored.</summary>
public interface IRefreshTokenStore
{
    /// <summary>Makes a token for the user, keeps its hash and returns the token (the only time it is seen).</summary>
    Task<string> IssueAsync(Guid userId, Guid tenantId, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses a token: when it is active it is revoked and a new one takes its place, and the user it was of is returned with the new token. <see langword="null"/> when there is no such token, it ended, or it was used before; a token that
    /// is used a second time is a token that was taken, so every token of its user is revoked.
    /// </summary>
    Task<(Guid UserId, string NewToken)?> RotateAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>Revokes every token of these users (their roles lost a subscription): they have to sign in again to get a new access token; the one they hold ends by itself.</summary>
    Task RevokeForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Revokes a token (the user signs out); nothing when there is no such token.</summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}
