using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hhs.AuthServer.Store;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
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

    public string CreateUserToken(AppUser user, List<string> roles, Tenant tenant, List<string> allowedTenantIds, Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey)
        => BaseCreateToken(client, clientReturnScopes, expirationSeconds, clientSecretKey, user, roles, tenant, allowedTenantIds);

    private string BaseCreateToken(Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey, AppUser user = null, List<string> roles = null, [CanBeNull] Tenant tenant = null, List<string> allowedTenantIds = null)
        => new JwtSecurityTokenHandler()
            .WriteToken(CreateJwtToken(
                CreateClaims(client, clientReturnScopes, user, roles, tenant, allowedTenantIds),
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

    private static IEnumerable<Claim> CreateClaims(Client client, List<string> clientReturnScopes, [CanBeNull] AppUser user, [CanBeNull] List<string> roles, [CanBeNull] Tenant tenant = null, List<string> allowedTenantIds = null)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")) };

        if (user != null)
        {
            claims.AddRange(new List<Claim>
            {
                // static claims
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                // db reference claims
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new(JwtRegisteredClaimNames.Email, user.Email),

                new(BaseClaimTypes.EmailVerified, user.EmailConfirmed.ToString()),
                new(BaseClaimTypes.PhoneNumber, user.PhoneNumber ?? string.Empty),
                new(BaseClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed.ToString()),

                new(JwtRegisteredClaimNames.GivenName, user.DisplayName ?? string.Empty),
                new(JwtRegisteredClaimNames.FamilyName, user.DisplayName ?? string.Empty),

                new(BaseClaimTypes.SecurityStamp, user.SecurityStamp),
                new(BaseClaimTypes.UserLanguage, user.LanguageCode ?? "en"),
            });

            if (roles is { Count: > 0 })
            {
                claims.AddRange(roles.Select(role => new Claim("role", role)));
            }
        }

        if (tenant != null)
        {
            claims.AddRange(new List<Claim>
            {
                new(BaseClaimTypes.TenantId, tenant.Id.ToString()),
                new(BaseClaimTypes.TenantNormalized, tenant.NormalizedName), //TODO : ENCRYPT
                new(BaseClaimTypes.IsSystemTenant, tenant.IsSystemTenant.ToString().ToLowerInvariant()),
            });

            if (!tenant.IsSystemTenant)
            {
                if (allowedTenantIds is { Count: > 0 })
                {
                    claims.AddRange(allowedTenantIds.Select(allowedTenantId => new Claim(BaseClaimTypes.AllowedTenantId, allowedTenantId.ToString())));
                }
            }
        }

        if (client != null)
        {
            claims.AddRange(new List<Claim> { new(BaseClaimTypes.ClientId, client.ClientId) });

            if (clientReturnScopes is not { Count: > 0 }) return claims;
            foreach (var scopeResources in clientReturnScopes.Select(scope => JwtClients.Scopes.FirstOrDefault(x => x.Name.Equals(scope)))
                         .Where(scopeResources => scopeResources?.Resources is { Count: > 0 }))
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