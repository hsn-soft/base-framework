using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Abc.AuthServer.Host.Options;
using HsnSoft.Base.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Abc.AuthServer.Host.Services;

public sealed record AccessToken(string Value, int ExpiresInSeconds);

/// <summary>
/// Checks the credentials and issues the short-lived JWT access token (claims: <c>sub</c> the user name, <c>role</c> the roles, <c>jti</c>), signed with the RSA <b>private</b> key
/// (RS256, <c>Jwt:PrivateKeyFile</c>) that only this host has; the other Abc hosts check the token with the public key (<c>Jwt:PublicKeyFile</c>) and cannot make one.
/// </summary>
public sealed class TokenService : IDisposable
{
    private readonly JwtOptions _jwt;
    private readonly AuthOptions _auth;
    private readonly RSA _privateKey;

    public TokenService(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth)
    {
        _jwt = jwt.Value;
        _auth = auth.Value;

        // loaded when the host starts (see Program.cs): a missing key is found now, not at the first login
        _privateKey = RsaKeys.LoadPrivate(_jwt.PrivateKeyFile ?? throw new InvalidOperationException("Jwt:PrivateKeyFile is required: the auth server signs the tokens with it."));
    }

    public void Dispose() => _privateKey.Dispose();

    public UserAccount? ValidateCredentials(string username, string password)
    {
        UserAccount? account = _auth.Users.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

        // constant-time comparison, and the same work whether or not the user exists
        byte[] expected = Encoding.UTF8.GetBytes(account?.Password ?? string.Empty);
        byte[] actual = Encoding.UTF8.GetBytes(password);
        bool equal = expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        return equal ? account : null;
    }

    public AccessToken Issue(UserAccount account)
    {
        JwtOptions settings = _jwt;
        List<Claim> claims = [new("sub", account.Username), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")), .. account.Roles.Select(role => new Claim("role", role))];

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
