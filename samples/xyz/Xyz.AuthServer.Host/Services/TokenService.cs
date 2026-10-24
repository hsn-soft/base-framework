using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HsnSoft.Base.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Xyz.AuthServer.Host.Services;

public sealed record TokenPair(string AccessToken, int ExpiresInSeconds, string RefreshToken);

/// <summary>Issues short-lived JWT access tokens and long-lived opaque refresh tokens. Refresh tokens live in redis.</summary>
public sealed class TokenService(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth, IDistributedCache cache)
{
    private const string RefreshPrefix = "refresh:";

    public UserAccount? ValidateCredentials(string username, string password)
    {
        UserAccount? account = auth.Value.Users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));

        // constant-time comparison, and the same work whether or not the user exists
        byte[] expected = Encoding.UTF8.GetBytes(account?.Password ?? string.Empty);
        byte[] actual = Encoding.UTF8.GetBytes(password);
        bool equal = expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        return equal ? account : null;
    }

    public async Task<TokenPair> IssueAsync(UserAccount account, CancellationToken cancellationToken)
    {
        JwtOptions settings = jwt.Value;
        DateTime expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim> { new("sub", account.Username), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")) };
        claims.AddRange(account.Roles.Select(role => new Claim("role", role)));

        string accessToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256),
        });

        string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await cache.SetStringAsync(
            RefreshPrefix + refreshToken,
            account.Username,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(auth.Value.RefreshTokenDays) },
            cancellationToken);

        return new TokenPair(accessToken, settings.AccessTokenMinutes * 60, refreshToken);
    }

    /// <summary>Exchanges a refresh token for a new pair. The old refresh token stops working (rotation).</summary>
    public async Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string? username = await cache.GetStringAsync(RefreshPrefix + refreshToken, cancellationToken);
        UserAccount? account = username is null ? null : auth.Value.Users.FirstOrDefault(x => x.Username == username);
        if (account is null)
        {
            return null;
        }

        await cache.RemoveAsync(RefreshPrefix + refreshToken, cancellationToken);
        return await IssueAsync(account, cancellationToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
        => await cache.RemoveAsync(RefreshPrefix + refreshToken, cancellationToken);
}
