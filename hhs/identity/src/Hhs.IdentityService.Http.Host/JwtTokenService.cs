using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Interfaces;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hhs.IdentityService;


public sealed class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public int ExpireMinutes { get; set; } = 60;
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly AuthServiceDbContext _db;
    private readonly JwtOptions _options;

    public JwtTokenService(AuthServiceDbContext db, IOptions<JwtOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<LoginResponse> CreateTokenAsync(AuthUser user)
    {
        var dbUser = await _db.AuthUsers
            .Include(x => x.Tenant)
            .FirstAsync(x => x.Id == user.Id);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpireMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, dbUser.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
            new(ClaimTypes.Name, dbUser.UserName),
            new(ClaimTypes.Email, dbUser.Email),
            new("tenant_id", dbUser.TenantId.ToString()),
            new("is_system_tenant", dbUser.Tenant.IsSystemTenant.ToString().ToLowerInvariant()),
            new("security_stamp", dbUser.SecurityStamp)
        };

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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            RefreshToken = TokenHelper.CreateRawToken(),
            ExpiresAt = expires
        };
    }
}