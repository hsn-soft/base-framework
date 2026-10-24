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

/// <summary>Checks the credentials and issues the short-lived JWT access token (claims: <c>sub</c> the user name, <c>role</c> the roles, <c>jti</c>) that the other Abc hosts validate with the same <c>Jwt</c> settings.</summary>
public sealed class TokenService(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth)
{
    public UserAccount? ValidateCredentials(string username, string password)
    {
        UserAccount? account = auth.Value.Users.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

        // constant-time comparison, and the same work whether or not the user exists
        byte[] expected = Encoding.UTF8.GetBytes(account?.Password ?? string.Empty);
        byte[] actual = Encoding.UTF8.GetBytes(password);
        bool equal = expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        return equal ? account : null;
    }

    public AccessToken Issue(UserAccount account)
    {
        JwtOptions settings = jwt.Value;
        List<Claim> claims = [new("sub", account.Username), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")), .. account.Roles.Select(role => new Claim("role", role))];

        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256),
        });
        return new AccessToken(token, settings.AccessTokenMinutes * 60);
    }
}
