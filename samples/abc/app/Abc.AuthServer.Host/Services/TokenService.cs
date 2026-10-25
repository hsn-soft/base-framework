using System.Security.Claims;
using System.Security.Cryptography;
using Abc.AuthServer.Host.Security;
using HsnSoft.Base.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Abc.AuthServer.Host.Services;

public sealed record AccessToken(string Value, int ExpiresInSeconds);

/// <summary>
/// Issues the short-lived JWT access token of a user the auth server has checked (claims: <c>sub</c> the user name, <c>user_id</c>, <c>name</c>, <c>email</c>, <c>tenant_id</c>, <c>locale</c> the language of the user, <c>is_system_tenant</c> for the users of the system tenant, <c>role</c> one per role, <c>jti</c>), signed with the RSA <b>private</b> key
/// (RS256, <c>Jwt:PrivateKeyFile</c>) that only this host has; the other Abc hosts check the token with the public key (<c>Jwt:PublicKeyFile</c>) and cannot make one.
/// </summary>
public sealed class TokenService : IDisposable
{
    private readonly JwtOptions _jwt;
    private readonly RSA _privateKey;

    public TokenService(IOptions<JwtOptions> jwt)
    {
        _jwt = jwt.Value;

        // loaded when the host starts (see Program.cs): a missing key is found now, not at the first login
        _privateKey = RsaKeys.LoadPrivate(_jwt.PrivateKeyFile ?? throw new InvalidOperationException("Jwt:PrivateKeyFile is required: the auth server signs the tokens with it."));
    }

    public void Dispose() => _privateKey.Dispose();

    public AccessToken Issue(AuthenticatedUser user)
    {
        JwtOptions settings = _jwt;
        List<Claim> claims =
        [
            new("sub", user.UserName),
            new("user_id", user.Id.ToString()),
            new("name", user.DisplayName),
            new("email", user.Email),
            new("tenant_id", user.TenantId.ToString()),
            new("locale", user.Language),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            .. user.Roles.Select(role => new Claim("role", role)),
        ];
        if (user.IsSystemTenant)
        {
            claims.Add(new Claim("is_system_tenant", "true"));
        }


        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256),
        });
        return new AccessToken(token, settings.AccessTokenMinutes * 60);
    }
}
