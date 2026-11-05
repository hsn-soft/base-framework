using System.Security.Cryptography;
using System.Text;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The refresh tokens: only the hash of a token is kept. A refresh token is not isolated by the tenant: the client that asks for a new token has nothing but the token.</summary>
internal sealed class EfCoreAuthRefreshTokenRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AuthRefreshToken, Guid>(db), IAuthRefreshTokenRepository
{
    public async Task<string> IssueAsync(Guid userId, Guid tenantId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        string token = NewToken();
        await AddAsync(AuthRefreshToken.Create(userId, tenantId, Hash(token), TimeProvider.System.GetUtcNow(), lifetime), cancellationToken).ConfigureAwait(false);
        return token;
    }

    public async Task<(Guid UserId, string NewToken)?> RotateAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        string hash = Hash(token);
        // read without tracking: the state of the row is changed by commands below, and a tracked copy would keep the old one
        IReadOnlyList<AuthRefreshToken> matches = await GetListAsync(new ListQueryOptions<AuthRefreshToken> { Filter = candidate => candidate.TokenHash == hash, MaxResultCount = 1 }, cancellationToken).ConfigureAwait(false);
        AuthRefreshToken? found = matches.Count > 0 ? matches[0] : null;
        if (found is null)
        {
            return null;
        }

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        string next = NewToken();
        string nextHash = Hash(next);

        // the token is taken in one command: of two calls with the same token, one changes the row and the other changes nothing
        int taken = await UpdateWhereAsync(
            candidate => candidate.Id == found.Id && candidate.RevokedAt == null && candidate.ExpiresAt > now,
            set => set.SetProperty(candidate => candidate.RevokedAt, now).SetProperty(candidate => candidate.ReplacedByTokenHash, nextHash),
            cancellationToken).ConfigureAwait(false);
        if (taken == 0)
        {
            // a token that was used before is a token that was taken: every token of the user ends
            if (found.RevokedAt is not null)
            {
                await UpdateWhereAsync(candidate => candidate.UserId == found.UserId && candidate.RevokedAt == null, set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        await AddAsync(AuthRefreshToken.Create(found.UserId, found.TenantId, nextHash, now, lifetime), cancellationToken).ConfigureAwait(false);
        return (found.UserId, next);
    }

    public async Task RevokeForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        await UpdateWhereAsync(candidate => userIds.Contains(candidate.UserId) && candidate.RevokedAt == null, set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        string hash = Hash(token);
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        await UpdateWhereAsync(candidate => candidate.TokenHash == hash && candidate.RevokedAt == null, set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
    }

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
