using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Utils;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

public static class AuthSeeder
{
    private const string DefaultPassword = "Admin1234";

    public static async Task SeedAsync(AuthServiceDbContext db, IPasswordHasher passwordHasher)
    {
        var systemTenant = await GetOrCreateTenantAsync(db,
            title: TenantConsts.SystemTenantName,
            name: TenantConsts.SystemTenantName,
            isSystemTenant: true,
            tenantId: Guid.Parse(TenantConsts.SystemTenantId),
            parentId: null,
            parentPath: null);

        // system   Parent: null   Path: /
        // A   Parent: null   Path: /A
        // DD  Parent: A      Path: /A/DD
        // CC  Parent: A      Path: /A/CC
        // E   Parent: CC     Path: /A/CC/E
        // F   Parent: E      Path: /A/CC/E/F

        var resellerTenant = await GetOrCreateTenantAsync(db,
            title: "Reseller A.Ş.",
            name: "reseller",
            isSystemTenant: false,
            tenantId: Guid.Parse("D729556C-93C4-4A1B-A550-2CE4794A28E7"),
            parentId: null,
            parentPath: null);

        var gazeteTenant = await GetOrCreateTenantAsync(db,
            title: "Gazete A.Ş.",
            name: "gazete",
            isSystemTenant: false,
            tenantId: Guid.Parse("C35FD197-E1FE-455D-B118-38404A69931E"),
            parentId: resellerTenant.Id,
            parentPath: resellerTenant.NormalizedAccessPath);

        var sporTenant = await GetOrCreateTenantAsync(db,
            title: "Spor A.Ş.",
            name: "spor",
            isSystemTenant: false,
            tenantId: Guid.Parse("B109FAF7-5AA2-451C-AC3A-51682D6C06E6"),
            parentId: resellerTenant.Id,
            parentPath: resellerTenant.NormalizedAccessPath);

        // ================ TECHSUMMUS TENATS ================

        var techsummusTenant = await GetOrCreateTenantAsync(db,
            title: "Tech Summus A.Ş.",
            name: "techsummus",
            isSystemTenant: false,
            tenantId: Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede"),
            parentId: null,
            parentPath: null);

        var dunyaTenant = await GetOrCreateTenantAsync(db,
            title: "Dünya A.Ş.",
            name: "dunya",
            isSystemTenant: false,
            tenantId: Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5"),
            parentId: null,
            parentPath: null);

        var kisadalgaTenant = await GetOrCreateTenantAsync(db,
            title: "Kısa Dalga A.Ş.",
            name: "kisadalga",
            isSystemTenant: false,
            tenantId: Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395"),
            parentId: null,
            parentPath: null);

        var tamindirTenant = await GetOrCreateTenantAsync(db,
            title: "Tam İndir A.Ş.",
            name: "tamindir",
            isSystemTenant: false,
            tenantId: Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17"),
            parentId: null,
            parentPath: null);

        var technotodayTenant = await GetOrCreateTenantAsync(db,
            title: "Techno Today A.Ş.",
            name: "technotoday",
            isSystemTenant: false,
            tenantId: Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717"),
            parentId: null,
            parentPath: null);

        var sondakikaTenant = await GetOrCreateTenantAsync(db,
            title: "Son Dakika A.Ş.",
            name: "sondakika",
            isSystemTenant: false,
            tenantId: Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7"),
            parentId: null,
            parentPath: null);

        var t24Tenant = await GetOrCreateTenantAsync(db,
            title: "T24 A.Ş.",
            name: "t24",
            isSystemTenant: false,
            tenantId: Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9"),
            parentId: null,
            parentPath: null);

        var cnbceTenant = await GetOrCreateTenantAsync(db,
            title: "Cnbce",
            name: "cnbce",
            isSystemTenant: false,
            tenantId: Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a"),
            parentId: null,
            parentPath: null);

        var boxofficeturkiyeTenant = await GetOrCreateTenantAsync(db,
            title: "Box Office Turkiye",
            name: "boxofficeturkiye",
            isSystemTenant: false,
            tenantId: Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c"),
            parentId: null,
            parentPath: null);

        // EnsureSystemRoles
        await EnsureRoleAsync(db, systemTenant.Id, DefaultRoleNames.SystemAdmin,true,false);
        await EnsureRoleAsync(db, systemTenant.Id, DefaultRoleNames.SystemUser,true,false);
        await EnsureRoleAsync(db, systemTenant.Id, DefaultRoleNames.RegisteredUser,true,true);

        // EnsureBusinessTenantRoles
        await EnsureRoleAsync(db, resellerTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, gazeteTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, sporTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, techsummusTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, dunyaTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, kisadalgaTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, tamindirTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, technotodayTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, sondakikaTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, t24Tenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, cnbceTenant.Id, DefaultRoleNames.TenantAdmin,true,true);
        await EnsureRoleAsync(db, boxofficeturkiyeTenant.Id, DefaultRoleNames.TenantAdmin,true,true);

        // EnsureDefaultPasswordPolicies
        await EnsureDefaultPasswordPolicyAsync(db, systemTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, resellerTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, gazeteTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, sporTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, techsummusTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, dunyaTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, kisadalgaTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, tamindirTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, technotodayTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, sondakikaTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, t24Tenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, cnbceTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, boxofficeturkiyeTenant.Id);

        // EnsureSystemUsers
        await EnsureUserAsync(db, passwordHasher, systemTenant.Id, DefaultUserNames.SystemAdmin, $"{DefaultUserNames.Admin}@local.dev", DefaultRoleNames.SystemAdmin);
        await EnsureUserAsync(db, passwordHasher, systemTenant.Id, DefaultUserNames.SystemUser, $"{DefaultUserNames.User}@local.dev", DefaultRoleNames.SystemUser);
        await EnsureUserAsync(db, passwordHasher, systemTenant.Id, "hsnsh", "hsnsh@outlook.com", DefaultRoleNames.RegisteredUser);

        // EnsureTenantAdminUsers
        await EnsureUserAsync(db, passwordHasher, resellerTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{resellerTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, gazeteTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{gazeteTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, sporTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{sporTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, techsummusTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{techsummusTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, dunyaTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{dunyaTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, kisadalgaTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{kisadalgaTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, tamindirTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{tamindirTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, technotodayTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{technotodayTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, sondakikaTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{sondakikaTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, t24Tenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{t24Tenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, cnbceTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{cnbceTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, passwordHasher, boxofficeturkiyeTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{boxofficeturkiyeTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);

        // update changes
        await db.SaveChangesAsync();
    }

    private static async Task<AuthTenant> GetOrCreateTenantAsync(AuthServiceDbContext db, string title, string name, bool isSystemTenant, Guid? tenantId = null, Guid? parentId = null, string parentPath = null)
    {
        string normalizedName = Normalize(name);

        var tenant = await db.AuthTenants.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        if (tenant is not null)
            return tenant;

        // system   Parent: null   Path: /
        // A   Parent: null   Path: /A
        // DD  Parent: A      Path: /A/DD
        // CC  Parent: A      Path: /A/CC
        // E   Parent: CC     Path: /A/CC/E
        // F   Parent: E      Path: /A/CC/E/F

        tenantId ??= Guid.CreateVersion7();
        string normalizedAccessPath = parentId == null
            ? isSystemTenant ? "/" : $"/{normalizedName}"
            : $"/{parentPath}/{normalizedName}";

        tenant = new AuthTenant
        {
            Id = tenantId.Value,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ParentId = parentId,
            Title = title,
            Name = name,
            NormalizedName = normalizedName,
            NormalizedAccessPath = normalizedAccessPath,
            IsSystemTenant = isSystemTenant,
        };

        db.AuthTenants.Add(tenant);
        await db.SaveChangesAsync();

        return tenant;
    }

    private static async Task EnsureRoleAsync(AuthServiceDbContext db, Guid tenantId, string roleName, bool isStatic, bool isDefault)
    {
        string normalizedRoleName = Normalize(roleName);

        var role = await db.AuthRoles.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedName == normalizedRoleName);

        if (role is not null) return;

        role = new AuthRole
        {
            TenantId = tenantId,
            Name = roleName,
            NormalizedName = normalizedRoleName,
            IsStatic = isStatic,
            IsDefault = isDefault
        };

        db.AuthRoles.Add(role);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(AuthServiceDbContext db, IPasswordHasher passwordHasher, Guid tenantId, string userName, string email, string roleName)
    {
        string normalizedUserName = Normalize(userName);
        string normalizedEmail = Normalize(email);
        string normalizedRoleName = Normalize(roleName);

        var user = await db.AuthUsers.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedUserName == normalizedUserName);

        if (user is null)
        {
            user = new AuthUser
            {
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId,
                UserName = userName,
                NormalizedUserName = normalizedUserName,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash(DefaultPassword),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsStatic = true,
                EmailConfirmed = true,
                FailedLoginCount = 0,
                LockoutEndAt = null,
                LastLoginAt = null,
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
            db.AuthUserRoles.Add(new AuthUserRole { UserId = user.Id, RoleId = role.Id });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureDefaultPasswordPolicyAsync(AuthServiceDbContext db, Guid tenantId)
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

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}