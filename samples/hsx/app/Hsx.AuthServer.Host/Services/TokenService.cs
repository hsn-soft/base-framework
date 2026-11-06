using System.Security.Claims;
using System.Security.Cryptography;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.MultiTenancy;
using Hsx.AuthServer.Host.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hsx.AuthServer.Host.Services;

public sealed record AccessToken(string Value, int ExpiresInSeconds);

/// <summary>
/// Issues the short-lived JWT access token of a user the auth server has checked (claims: <c>sub</c> the user name, <c>user_id</c>, <c>name</c>, <c>email</c>, <c>tenant_id</c>, <c>locale</c> the language of the user, <c>allowed_tenant_id</c> one per tenant under its own, <c>allowed_scope_key</c> one per subscription of its roles, <c>is_system_tenant</c> for the users of the system tenant, <c>role</c> one per role, <c>jti</c>), signed with the RSA <b>private</b> key
/// (RS256, <c>Jwt:PrivateKeyFile</c>) that only this host has; the other Hsx hosts check the token with the public key (<c>Jwt:PublicKeyFile</c>) and cannot make one.
/// </summary>
public sealed class TokenService : IDisposable
{
    private readonly JwtOptions _jwt;
    private readonly RSA _privateKey;

    public TokenService(IOptions<JwtOptions> jwt)
    {
        _jwt = jwt.Value;
        if (_jwt.IssuedAudiences.Count == 0)
        {
            throw new InvalidOperationException("Jwt:IssuedAudiences is required: a token names the services it may be sent to, and a token with no audience is accepted by none.");
        }

        // loaded when the host starts (see Program.cs): a missing key is found now, not at the first login
        _privateKey = RsaKeys.LoadPrivate(_jwt.PrivateKeyFile ?? throw new InvalidOperationException("Jwt:PrivateKeyFile is required: the auth server signs the tokens with it."));
    }

    public void Dispose() => _privateKey.Dispose();

    /// <summary>Issues the token of <paramref name="user"/>. <paramref name="audiences"/> are the services the token may be sent to (<c>aud</c>): the ones of <c>Jwt:IssuedAudiences</c> when they are not given, so a client that may reach fewer services is given fewer; <paramref name="accessTokenMinutes"/> is how long it lives (<c>Jwt:AccessTokenMinutes</c> when it is not given).</summary>
    public AccessToken Issue(AuthenticatedUser user, IReadOnlyCollection<string>? audiences = null, int? accessTokenMinutes = null)
    {
        ArgumentNullException.ThrowIfNull(user);

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
            // the tenants under the tenant of the user, which its users see besides their own (the tenant of the user is always allowed, so it is not listed)
            .. user.AllowedTenantIds.Select(tenantId => new Claim(TenantClaimTypes.AllowedTenantId, tenantId.ToString())),
            // the subscriptions of its roles: the data of those products of those customers is what it may see
            .. user.ScopeKeys.Select(key => new Claim(TenantClaimTypes.AllowedScopeKey, key)),
        ];
        if (user.IsSystemTenant)
        {
            claims.Add(new Claim(TenantClaimTypes.IsSystemTenant, "true"));
        }

        return Sign(claims, audiences, accessTokenMinutes);
    }

    /// <summary>Issues the token of a client itself (no user): <c>sub</c> and <c>client_id</c> are the client, and there is no tenant, no role and no scope key, so a service that asks for a permission refuses it; one that asks only for a valid token with its audience lets it in.</summary>
    public AccessToken IssueForClient(string clientId, IReadOnlyCollection<string>? audiences = null, int? accessTokenMinutes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        return Sign([new Claim("sub", clientId), new Claim("client_id", clientId), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))], audiences, accessTokenMinutes);
    }

    private AccessToken Sign(List<Claim> claims, IReadOnlyCollection<string>? audiences, int? accessTokenMinutes)
    {
        int minutes = accessTokenMinutes is > 0 ? accessTokenMinutes.Value : _jwt.AccessTokenMinutes;
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Aud] = (audiences is { Count: > 0 } ? audiences : _jwt.IssuedAudiences).ToArray() },
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(minutes),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256),
        });
        return new AccessToken(token, minutes * 60);
    }
}
