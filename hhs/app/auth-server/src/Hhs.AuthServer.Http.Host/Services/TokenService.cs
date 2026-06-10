using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hhs.AuthServer.Store;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.Enums;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using HsnSoft.Base.Security.Claims;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

namespace Hhs.AuthServer.Services;

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

    public string CreateUserToken(AppUser user, List<AppRole> roles, Tenant tenant, List<string> allowedTenantIds,
        Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey)
        => BaseCreateToken(client, clientReturnScopes, expirationSeconds, clientSecretKey, user, roles, tenant, allowedTenantIds);

    private string BaseCreateToken(Client client, List<string> clientReturnScopes, int expirationSeconds, string clientSecretKey,
        AppUser user = null,
        List<AppRole> roles = null, [CanBeNull] Tenant tenant = null, List<string> allowedTenantIds = null)
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

    private static IEnumerable<Claim> CreateClaims(Client client, List<string> clientReturnScopes,
        [CanBeNull] AppUser user,
        [CanBeNull] List<AppRole> roles = null,
        [CanBeNull] Tenant tenant = null,
        List<string> allowedTenantIds = null)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("N")) };

        if (user != null)
        {
            claims.AddRange(new List<Claim>
            {
                // static claims
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                // db reference claims
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new(JwtRegisteredClaimNames.Email, user.Email),

                // new(BaseClaimTypes.EmailVerified, user.EmailConfirmed.ToString()),
                // new(BaseClaimTypes.PhoneNumber, user.PhoneNumber ?? string.Empty),
                // new(BaseClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed.ToString()),
                //
                // new(JwtRegisteredClaimNames.GivenName, user.DisplayName ?? string.Empty),
                // new(JwtRegisteredClaimNames.FamilyName, user.DisplayName ?? string.Empty),
                //
                // new(BaseClaimTypes.SecurityStamp, user.SecurityStamp),
                new(BaseClaimTypes.UserLanguage, user.LanguageCode ?? "en"),
            });

            if (roles is { Count: > 0 })
            {
                claims.AddRange(roles.Select(role => new Claim("role", role.Id.ToString("N"))));
            }

            // foreach (var role in roles)
            // {
            //     claims.Add(new Claim(ClaimTypes.Role, role.Name));
            //
            //     var roleClaims = await _db.AppRoleClaims
            //         .Where(x => x.RoleId == role.Id)
            //         .ToListAsync();
            //
            //     foreach (var roleClaim in roleClaims)
            //         claims.Add(new Claim(roleClaim.ClaimType, roleClaim.ClaimValue));
            // }
            //
            // var userClaims = await _db.AppUserClaims
            //     .Where(x => x.UserId == user.Id)
            //     .ToListAsync();
            //
            // foreach (var userClaim in userClaims)
            //     claims.Add(new Claim(userClaim.ClaimType, userClaim.ClaimValue));
        }

        if (tenant != null)
        {
            bool isSystemTenant = tenant.TenantType == TenantTypes.System;
            claims.AddRange(new List<Claim>
            {
                new(BaseClaimTypes.TenantId, tenant.Id.ToString()),
                new(BaseClaimTypes.TenantNormalized, tenant.NormalizedName), //TODO : ENCRYPT
                new(BaseClaimTypes.IsSystemTenant, isSystemTenant.ToString().ToLowerInvariant()),
            });

            if (!isSystemTenant)
            {
                if (allowedTenantIds is { Count: > 0 })
                {
                    claims.AddRange(allowedTenantIds.Select(allowedTenantId => new Claim(BaseClaimTypes.AllowedTenantId, allowedTenantId.ToString())));
                }

                if (roles is { Count: > 0 })
                {
                    var allowedSubscriptions = new List<HsnSoft.Base.Subscribe.Subscription>();
                    foreach (var role in roles)
                    {
                        allowedSubscriptions.AddRange(role.Subscriptions.Select(s
                            => new HsnSoft.Base.Subscribe.Subscription(
                                CustomerId: s.Subscription.CustomerId,
                                ProductTypeId: s.Subscription.ProductTypeId
                            )));
                    }

                    // var allowedSubscriptions = await dbContext.Set<UserAccessGrant>()
                    //     .AsNoTracking()
                    //     .Where(x =>
                    //         x.UserId == user.Id &&
                    //         x.TenantId == tenant.Id &&
                    //         x.IsActive)
                    //     .Where(x =>
                    //         x.ProductSubscription.IsActive &&
                    //         x.ProductSubscription.TenantId == tenant.Id)
                    //     .Select(x => new Subscription(
                    //         CustomerId: x.ProductSubscription.ClientId,
                    //         ProductTypeId: x.ProductSubscription.ProductTypeId
                    //     ))
                    //     .Distinct()
                    //     .ToListAsync(cancellationToken);
                    //
                    claims.Add(new Claim(BaseClaimTypes.AllowedSubscription, JsonSerializer.Serialize(allowedSubscriptions)));

                    // {
                    //     "tenant_id": "RESELLER-A",
                    //     "allowed_tenant_id": ["RESELLER-A"],
                    //     "allowed_contents": [
                    //     {
                    //         "clientId": "haberturk-client-id",
                    //         "productTypeId": "web-platform-id"
                    //     },
                    //     {
                    //         "clientId": "bloomberght-client-id",
                    //         "productTypeId": "web-platform-id"
                    //     },
                    //     {
                    //         "clientId": "bloomberght-client-id",
                    //         "productTypeId": "podcast-id"
                    //     }
                    //     ]
                    // }
                    //
                    // Buradaki allowed_contents ayrı tablodan gelmek zorunda değil.
                    //     ProductSubscription tablosundan üretilebilir.
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
}