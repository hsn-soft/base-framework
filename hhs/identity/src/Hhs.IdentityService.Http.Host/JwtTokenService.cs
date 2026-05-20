using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Interfaces;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hhs.IdentityService;

public sealed class JwtTokenService : IJwtTokenService
{
    private const string TokenHeaderId = "ODc2MjE3MTIxOQ";
    private readonly AuthServiceDbContext _db;

    public JwtTokenService(AuthServiceDbContext db)
    {
        _db = db;
    }

    public async Task<LoginResponse> CreateTokenAsync(AuthUser user)
    {
        var dbUser = await _db.AuthUsers
            .Include(x => x.Tenant)
            .FirstAsync(x => x.Id == user.Id);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(5);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, dbUser.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(BaseClaimTypes.SecurityStamp, dbUser.SecurityStamp),

            new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()), //TODO : ENCRYPT

            new(BaseClaimTypes.TenantId, dbUser.TenantId.ToString()),
            new(BaseClaimTypes.TenantNormalized, dbUser.Tenant.NormalizedName), //TODO : ENCRYPT
            new(BaseClaimTypes.IsSystemTenant, dbUser.Tenant.IsSystemTenant.ToString().ToLowerInvariant()),


            // TODO:Bu user için, yada role une ait claim var ise token a eklenecek.
            new(ClaimTypes.Name, dbUser.UserName), //TODO : ENCRYPT
            new(ClaimTypes.Email, dbUser.Email), //TODO : ENCRYPT
        };

        if (!dbUser.Tenant.IsSystemTenant)
        {
            var allowedTenantIds = await _db.AuthTenants
                .Where(x => x.NormalizedAccessPath.StartsWith(dbUser.Tenant.NormalizedAccessPath))
                .Select(x => x.Id)
                .ToListAsync();

            if (allowedTenantIds is { Count: > 0 })
            {
                claims.AddRange(allowedTenantIds.Select(allowedTenantId => new Claim(BaseClaimTypes.AllowedTenantId, allowedTenantId.ToString())));
            }
        }

        var roles = await _db.AuthUserRoles
            .Where(x => x.UserId == dbUser.Id)
            .Select(x => x.Role)
            .ToListAsync();

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

            var roleClaims = await _db.AuthRoleClaims
                .Where(x => x.RoleId == role.Id)
                .ToListAsync();

            foreach (var roleClaim in roleClaims)
                claims.Add(new Claim(roleClaim.ClaimType, roleClaim.ClaimValue));
        }

        var userClaims = await _db.AuthUserClaims
            .Where(x => x.UserId == dbUser.Id)
            .ToListAsync();

        foreach (var userClaim in userClaims)
            claims.Add(new Claim(userClaim.ClaimType, userClaim.ClaimValue));

        // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        // var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: "https://localhost:7101",
            // audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: CreateAsymetricSigningCredentials());

        return new LoginResponse { AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt), RefreshToken = TokenHelper.CreateRawToken(), ExpiresAt = expires };
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