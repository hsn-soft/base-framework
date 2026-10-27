using System.Security.Claims;
using System.Security.Cryptography;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xyz.AuthServer.Security;

namespace Xyz.AuthServer.Services;

public sealed record TokenPair(string AccessToken, int ExpiresInSeconds, string RefreshToken);

/// <summary>The settings of the refresh token (<c>Auth</c>).</summary>
public sealed class AuthOptions
{
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>
/// Issues the short-lived JWT access token of a user the auth server has checked (claims: <c>sub</c> the user name, <c>user_id</c>, <c>name</c>, <c>email</c>, <c>tenant_id</c>, <c>locale</c>, <c>is_system_tenant</c> for the users of
/// the system tenant, <c>role</c> one per role, <c>jti</c>), signed with the RSA <b>private</b> key (RS256, <c>Jwt:PrivateKeyFile</c>) that only this host has; the other Xyz hosts check it with the public key. The long-lived
/// opaque refresh token is kept in Redis (the id of its user) and used once: a refresh rotates it.
/// </summary>
public sealed class TokenService : IDisposable
{
    private const string RefreshPrefix = "refresh:";

    private readonly JwtOptions _jwt;
    private readonly AuthOptions _auth;
    private readonly IDistributedCache _cache;
    private readonly RSA _privateKey;

    public TokenService(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth, IDistributedCache cache)
    {
        _jwt = jwt.Value;
        _auth = auth.Value;
        _cache = cache;

        // loaded when the host starts (see Program.cs): a missing key is found now, not at the first login
        _privateKey = RsaKeys.LoadPrivate(_jwt.PrivateKeyFile ?? throw new InvalidOperationException("Jwt:PrivateKeyFile is required: the auth server signs the tokens with it."));
    }

    public void Dispose() => _privateKey.Dispose();

    public async Task<TokenPair> IssueAsync(AuthenticatedUser user, CancellationToken cancellationToken = default)
    {
        List<Claim> claims =
        [
            new("sub", user.UserName),
            new("user_id", user.Id.ToString()),
            new("name", user.DisplayName),
            new("email", user.Email),
            new(TenantClaimTypes.TenantId, user.TenantId.ToString()),
            new("locale", user.Language),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            .. user.Roles.Select(role => new Claim("role", role)),
        ];
        if (user.IsSystemTenant)
        {
            claims.Add(new Claim(TenantClaimTypes.IsSystemTenant, "true"));
        }

        string accessToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256),
        });

        string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _cache.SetStringAsync(
            RefreshPrefix + refreshToken,
            user.Id.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_auth.RefreshTokenDays) },
            cancellationToken).ConfigureAwait(false);

        return new TokenPair(accessToken, _jwt.AccessTokenMinutes * 60, refreshToken);
    }

    /// <summary>The id of the user a refresh token was issued to, <see langword="null"/> when it is unknown, expired or used. A token that is found is removed (rotation): it works once.</summary>
    public async Task<Guid?> ConsumeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        string? value = await _cache.GetStringAsync(RefreshPrefix + refreshToken, cancellationToken).ConfigureAwait(false);
        if (value is null)
        {
            return null;
        }

        await _cache.RemoveAsync(RefreshPrefix + refreshToken, cancellationToken).ConfigureAwait(false);
        return Guid.TryParse(value, out Guid id) ? id : null;
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default) => _cache.RemoveAsync(RefreshPrefix + refreshToken, cancellationToken);
}
