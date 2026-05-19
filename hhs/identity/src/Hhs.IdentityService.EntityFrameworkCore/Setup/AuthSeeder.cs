using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

public static class AuthSeeder
{
    private const string DefaultPassword = "Admin1234";

    public static async Task SeedAsync(AuthServiceDbContext db, IPasswordHasher passwordHasher)
    {
        var systemTenant = await GetOrCreateTenantAsync(db, "system", true);

        var aaTenant = await GetOrCreateTenantAsync(db, "AA", false);
        var bbTenant = await GetOrCreateTenantAsync(db, "BB", false);
        var xxxTenant = await GetOrCreateTenantAsync(db, "XXX", false);
        var yyyTenant = await GetOrCreateTenantAsync(db, "YYY", false);

        await EnsureSystemRolesAsync(db, systemTenant.Id);
        await EnsureBusinessTenantRolesAsync(db, aaTenant.Id);
        await EnsureBusinessTenantRolesAsync(db, bbTenant.Id);
        await EnsureBusinessTenantRolesAsync(db, xxxTenant.Id);
        await EnsureBusinessTenantRolesAsync(db, yyyTenant.Id);

        await EnsureDefaultPasswordPolicyAsync(db, systemTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, aaTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, bbTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, xxxTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, yyyTenant.Id);

        // await EnsureTenantAccessAsync(db, systemTenant.Id, systemTenant.Id);
        // await EnsureTenantAccessAsync(db, systemTenant.Id, aaTenant.Id);
        // await EnsureTenantAccessAsync(db, systemTenant.Id, bbTenant.Id);
        // await EnsureTenantAccessAsync(db, systemTenant.Id, xxxTenant.Id);
        // await EnsureTenantAccessAsync(db, systemTenant.Id, yyyTenant.Id);
        //
        // await EnsureTenantAccessAsync(db, aaTenant.Id, aaTenant.Id);
        // await EnsureTenantAccessAsync(db, aaTenant.Id, xxxTenant.Id);
        // await EnsureTenantAccessAsync(db, aaTenant.Id, yyyTenant.Id);
        //
        // await EnsureTenantAccessAsync(db, bbTenant.Id, bbTenant.Id);
        // await EnsureTenantAccessAsync(db, xxxTenant.Id, xxxTenant.Id);
        // await EnsureTenantAccessAsync(db, yyyTenant.Id, yyyTenant.Id);

        await EnsureUserAsync(
            db,
            passwordHasher,
            systemTenant.Id,
            "admin",
            "system-admin@local.dev",
            "system-admin");

        await EnsureUserAsync(
            db,
            passwordHasher,
            systemTenant.Id,
            "operator",
            "system-operator@local.dev",
            "system-user");

        await EnsureUserAsync(
            db,
            passwordHasher,
            systemTenant.Id,
            "hsnsh",
            "hsnsh@local.dev",
            "registered");

        await EnsureUserAsync(
            db,
            passwordHasher,
            aaTenant.Id,
            "admin",
            "aa-admin@local.dev",
            "manager");

        await EnsureUserAsync(
            db,
            passwordHasher,
            bbTenant.Id,
            "admin",
            "bb-admin@local.dev",
            "manager");

        await EnsureUserAsync(
            db,
            passwordHasher,
            xxxTenant.Id,
            "admin",
            "xxx-admin@local.dev",
            "manager");

        await EnsureUserAsync(
            db,
            passwordHasher,
            yyyTenant.Id,
            "admin",
            "yyy-admin@local.dev",
            "manager");

        await db.SaveChangesAsync();
    }

    private static async Task<AuthTenant> GetOrCreateTenantAsync(
        AuthServiceDbContext db,
        string name,
        bool isSystemTenant)
    {
        var normalizedName = Normalize(name);

        var tenant = await db.AuthTenants
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        if (tenant is not null)
            return tenant;

        tenant = new AuthTenant
        {
            Name = name,
            NormalizedName = normalizedName,
            IsSystemTenant = isSystemTenant,
            IsActive = true
        };

        db.AuthTenants.Add(tenant);
        await db.SaveChangesAsync();

        return tenant;
    }

    private static async Task EnsureSystemRolesAsync(
        AuthServiceDbContext db,
        Guid tenantId)
    {
        await EnsureRoleAsync(db, tenantId, "system-admin");
        await EnsureRoleAsync(db, tenantId, "system-user");
        await EnsureRoleAsync(db, tenantId, "registered");

        await EnsureRoleClaimAsync(db, tenantId, "system-admin", "permission", "tenant.full_access");
        await EnsureRoleClaimAsync(db, tenantId, "system-admin", "permission", "user.manage");
        await EnsureRoleClaimAsync(db, tenantId, "system-admin", "permission", "role.manage");

        await EnsureRoleClaimAsync(db, tenantId, "system-user", "permission", "user.read");

        await EnsureRoleClaimAsync(db, tenantId, "registered", "permission", "profile.read");
    }

    private static async Task EnsureBusinessTenantRolesAsync(
        AuthServiceDbContext db,
        Guid tenantId)
    {
        await EnsureRoleAsync(db, tenantId, "manager");
        await EnsureRoleAsync(db, tenantId, "operator");
        await EnsureRoleAsync(db, tenantId, "registered");

        await EnsureRoleClaimAsync(db, tenantId, "manager", "permission", "tenant.manage");
        await EnsureRoleClaimAsync(db, tenantId, "manager", "permission", "user.manage");
        await EnsureRoleClaimAsync(db, tenantId, "manager", "permission", "role.manage");

        await EnsureRoleClaimAsync(db, tenantId, "operator", "permission", "user.read");

        await EnsureRoleClaimAsync(db, tenantId, "registered", "permission", "profile.read");
    }

    private static async Task<AuthRole> EnsureRoleAsync(
        AuthServiceDbContext db,
        Guid tenantId,
        string roleName)
    {
        var normalizedRoleName = Normalize(roleName);

        var role = await db.AuthRoles.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedName == normalizedRoleName);

        if (role is not null)
            return role;

        role = new AuthRole
        {
            TenantId = tenantId,
            Name = roleName,
            NormalizedName = normalizedRoleName
        };

        db.AuthRoles.Add(role);
        await db.SaveChangesAsync();

        return role;
    }

    private static async Task EnsureRoleClaimAsync(
        AuthServiceDbContext db,
        Guid tenantId,
        string roleName,
        string claimType,
        string claimValue)
    {
        var role = await EnsureRoleAsync(db, tenantId, roleName);

        bool exists = await db.AuthRoleClaims.AnyAsync(x =>
            x.RoleId == role.Id &&
            x.ClaimType == claimType &&
            x.ClaimValue == claimValue);

        if (exists)
            return;

        db.AuthRoleClaims.Add(new AuthRoleClaim
        {
            RoleId = role.Id,
            ClaimType = claimType,
            ClaimValue = claimValue
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        AuthServiceDbContext db,
        IPasswordHasher passwordHasher,
        Guid tenantId,
        string userName,
        string email,
        string roleName)
    {
        var normalizedUserName = Normalize(userName);
        var normalizedEmail = Normalize(email);
        var normalizedRoleName = Normalize(roleName);

        var user = await db.AuthUsers.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedUserName == normalizedUserName);

        if (user is null)
        {
            user = new AuthUser
            {
                TenantId = tenantId,
                UserName = userName,
                NormalizedUserName = normalizedUserName,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash(DefaultPassword),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            db.AuthUsers.Add(user);
            await db.SaveChangesAsync();
        }

        var role = await db.AuthRoles.FirstAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedName == normalizedRoleName);

        bool hasRole = await db.AuthUserRoles.AnyAsync(x =>
            x.UserId == user.Id &&
            x.RoleId == role.Id);

        if (!hasRole)
        {
            db.AuthUserRoles.Add(new AuthUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureDefaultPasswordPolicyAsync(
        AuthServiceDbContext db,
        Guid tenantId)
    {
        bool exists = await db.AuthPasswordPolicies.AnyAsync(x => x.TenantId == tenantId);

        if (exists)
            return;

        db.AuthPasswordPolicies.Add(new AuthPasswordPolicy
        {
            TenantId = tenantId,
            MinLength = 8,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = false,
            MaxFailedLoginCount = 5,
            LockoutMinutes = 15
        });

        await db.SaveChangesAsync();
    }

    // private static async Task EnsureTenantAccessAsync(
    //     AuthServiceDbContext db,
    //     Guid sourceTenantId,
    //     Guid targetTenantId)
    // {
    //     bool exists = await db.AuthTenantAccesses.AnyAsync(x =>
    //         x.SourceTenantId == sourceTenantId &&
    //         x.TargetTenantId == targetTenantId);
    //
    //     if (exists)
    //         return;
    //
    //     db.AuthTenantAccesses.Add(new AuthTenantAccess
    //     {
    //         SourceTenantId = sourceTenantId,
    //         TargetTenantId = targetTenantId
    //     });
    //
    //     await db.SaveChangesAsync();
    // }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}