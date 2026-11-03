using System.Security.Cryptography;
using System.Text;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreRefreshTokenStore(IdentityDbContext db) : IRefreshTokenStore
{
    public async Task<string> IssueAsync(Guid userId, Guid tenantId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        string token = NewToken();
        db.AuthRefreshTokens.Add(AuthRefreshToken.Create(userId, tenantId, Hash(token), TimeProvider.System.GetUtcNow(), lifetime));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return token;
    }

    public async Task<(Guid UserId, string NewToken)?> RotateAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        string hash = Hash(token);
        AuthRefreshToken? found = await db.AuthRefreshTokens.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken).ConfigureAwait(false);
        if (found is null)
        {
            return null;
        }

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        string next = NewToken();
        string nextHash = Hash(next);

        // the token is taken in one command: of two calls with the same token, one changes the row and the other changes nothing
        int taken = await db.AuthRefreshTokens
            .Where(candidate => candidate.Id == found.Id && candidate.RevokedAt == null && candidate.ExpiresAt > now)
            .ExecuteUpdateAsync(set => set.SetProperty(candidate => candidate.RevokedAt, now).SetProperty(candidate => candidate.ReplacedByTokenHash, nextHash), cancellationToken).ConfigureAwait(false);
        if (taken == 0)
        {
            // a token that was used before is a token that was taken: every token of the user ends
            if (found.RevokedAt is not null)
            {
                await db.AuthRefreshTokens.Where(candidate => candidate.UserId == found.UserId && candidate.RevokedAt == null)
                    .ExecuteUpdateAsync(set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        db.AuthRefreshTokens.Add(AuthRefreshToken.Create(found.UserId, found.TenantId, nextHash, now, lifetime));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (found.UserId, next);
    }

    public async Task RevokeForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        await db.AuthRefreshTokens.Where(candidate => userIds.Contains(candidate.UserId) && candidate.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        string hash = Hash(token);
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        await db.AuthRefreshTokens.Where(candidate => candidate.TokenHash == hash && candidate.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(candidate => candidate.RevokedAt, now), cancellationToken).ConfigureAwait(false);
    }

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
