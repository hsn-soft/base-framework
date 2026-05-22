using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hhs.AuthServer.Store;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Security.Claims;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

namespace Hhs.AuthServer;

public sealed class TokenService
{
    private const string TokenHeaderId = "ODc2MjE3MTIxOQ";
    private IConfiguration Configuration { get; }

    public TokenService(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public string CreateM2MToken(Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey)
        => BaseCreateToken(client, clientReturnScopes, expirationSeconds, clientSecretKey);

    public string CreateUserToken(AppUser user, List<string> roles, Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey)
        => BaseCreateToken(client, clientReturnScopes, expirationSeconds, clientSecretKey, user, roles);

    private string BaseCreateToken(Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey, AppUser user = null, List<string> roles = null)
        => new JwtSecurityTokenHandler()
            .WriteToken(CreateJwtToken(
                CreateClaims(client, clientReturnScopes, user, roles),
                // CreateSymetricSigningCredentials(clientSecretKey),
                CreateAsymetricSigningCredentials(),
                expirationSeconds
            ));

    private JwtSecurityToken CreateJwtToken(IEnumerable<Claim> claims, SigningCredentials credentials, int expirationSeconds) => new
    (
        issuer: Configuration["App:SelfUrl"],
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddSeconds(expirationSeconds),
        signingCredentials: credentials,
        claims: claims
    );

    private static IEnumerable<Claim> CreateClaims(Client client, List<string> clientReturnScopes, [CanBeNull] AppUser user, [CanBeNull] List<string> roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N").ToUpper()) };

        if (user != null)
        {
            claims.AddRange(new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Name, user.DisplayName ?? string.Empty),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("user_lg", user.LanguageCode ?? string.Empty),

                new(BaseClaimTypes.TenantId, user.TenantId.ToString()),
                // new(BaseClaimTypes.TenantDomain, user.TenantDomain ?? string.Empty),
                // new("user_s", user.IsSystemUser.ToString(), ClaimValueTypes.Boolean),
                // new("user_t", user.IsTenantUser.ToString(), ClaimValueTypes.Boolean),
            });

            if (roles is { Count: > 0 })
            {
                claims.AddRange(roles.Select(role => new Claim("role", role)));
            }
        }

        if (client == null) return claims;
        claims.AddRange(new List<Claim> { new("client_id", client.ClientId) });

        if (clientReturnScopes is not { Count: > 0 }) return claims;
        foreach (string scope in clientReturnScopes)
        {
            claims.Add(new Claim("scope", scope));
            // add scope resources
            var scopeResources = JwtClients.Scopes.FirstOrDefault(x => x.Name.Equals(scope));
            if (scopeResources?.Resources is { Count: > 0 })
            {
                claims.AddRange(scopeResources.Resources.Select(resource =>
                    new Claim(JwtRegisteredClaimNames.Aud, resource)));
            }
        }

        return claims;
    }

    private static SigningCredentials CreateSymetricSigningCredentials([NotNull] string clientSecretKey)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecretKey)) { KeyId = TokenHeaderId };

        return new SigningCredentials(key: securityKey, algorithm: SecurityAlgorithms.HmacSha256);
    }

    private static SigningCredentials CreateAsymetricSigningCredentials()
    {
        var rsa = RSA.Create();

        try
        {
            rsa.FromXmlString(File.ReadAllText(AppContext.BaseDirectory + "/private_key.xml"));
        }
        catch (IOException ioException)
        {
            throw new Exception("You need to provide private_key.xml to use auth", ioException);
        }

        var securityKey = new RsaSecurityKey(rsa) { KeyId = TokenHeaderId };

        return new SigningCredentials(key: securityKey, algorithm: SecurityAlgorithms.RsaSha256); // Important to use RSA version of the SHA algo
    }

    public string CreateRefreshToken()
    {
        byte[] number = new byte[32];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(number);
        return Convert.ToBase64String(number);
    }
}