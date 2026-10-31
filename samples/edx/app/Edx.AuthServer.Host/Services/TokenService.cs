using System.Security.Claims;
using System.Security.Cryptography;
using Edx.AuthServer.Host.Security;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Edx.AuthServer.Host.Services;

public sealed record AccessToken(string Value, int ExpiresInSeconds);

/// <summary>
/// Issues the short-lived JWT access token of a user the auth server has checked (claims: <c>sub</c> the user name, <c>user_id</c>, <c>name</c>, <c>email</c>, <c>tenant_id</c>, <c>is_system_tenant</c> for the users of the system tenant, <c>role</c> one per role, <c>jti</c>), signed with the RSA <b>private</b> key
/// (RS256, <c>Jwt:PrivateKeyFile</c>) that only this host has; the other Edx hosts check the token with the public key (<c>Jwt:PublicKeyFile</c>) and cannot make one.
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

    /// <summary>Issues the token of <paramref name="user"/>. <paramref name="audiences"/> are the services the token may be sent to (<c>aud</c>): the ones of <c>Jwt:IssuedAudiences</c> when they are not given, so a client that may reach fewer services is given fewer.</summary>
    public AccessToken Issue(AuthenticatedUser user, IReadOnlyCollection<string>? audiences = null)
    {
        JwtOptions settings = _jwt;
        List<Claim> claims =
        [
            new("sub", user.UserName),
            new("user_id", user.Id.ToString()),
            new("name", user.DisplayName),
            new("email", user.Email),
            new(TenantClaimTypes.TenantId, user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            .. user.Roles.Select(role => new Claim("role", role)),
        ];
        if (user.IsSystemTenant)
        {
            claims.Add(new Claim(TenantClaimTypes.IsSystemTenant, "true"));
        }

        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Aud] = (audiences is { Count: > 0 } ? audiences : settings.IssuedAudiences).ToArray() },
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256),
        });
        return new AccessToken(token, settings.AccessTokenMinutes * 60);
    }
}
