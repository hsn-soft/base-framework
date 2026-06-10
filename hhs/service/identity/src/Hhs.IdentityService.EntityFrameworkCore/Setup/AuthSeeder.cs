using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.Enums;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

public static class AuthSeeder
{
    private const string DefaultPassword = "Passw0rd!";

    public static async Task SeedAsync(IdentityServiceDbContext db, IAppConsoleLogger logger, IPasswordHasher passwordHasher)
    {
        var systemTenant = await GetOrCreateTenantAsync(db, logger,
            tenantType: TenantTypes.System,
            title: TenantConsts.SystemTenantName,
            name: TenantConsts.SystemTenantName,
            tenantId: Guid.Parse(TenantConsts.SystemTenantId),
            parentId: null,
            parentPath: null);

        // system   Parent: null   Path: /
        // A   Parent: null   Path: /A
        // DD  Parent: A      Path: /A/DD
        // CC  Parent: A      Path: /A/CC
        // E   Parent: CC     Path: /A/CC/E
        // F   Parent: E      Path: /A/CC/E/F

        var techsummusTenant = await GetOrCreateTenantAsync(db, logger,
            tenantType: TenantTypes.Reseller,
            title: "Tech Summus A.Ş.",
            name: "Tech Summus",
            tenantId: Guid.Parse(TenantIds.TechsummusTenantId));

        var audioTimeTenant = await GetOrCreateTenantAsync(db, logger,
            tenantType: TenantTypes.Reseller,
            title: "Audio Time A.Ş.",
            name: "Audio Time",
            tenantId: Guid.Parse(TenantIds.AudioTimeTenantId));

        var efaturaPartnerTenant = await GetOrCreateTenantAsync(db, logger,
            tenantType: TenantTypes.Reseller,
            title: "Efatura Partner A.Ş.",
            name: "ePartner",
            tenantId: Guid.Parse(TenantIds.EfaturaPartnerTenantId));

        var ilbakAccountTenant = await GetOrCreateTenantAsync(db, logger,
            tenantType: TenantTypes.Account,
            title: "Ilbak Account Tenant",
            name: "Ilbak",
            tenantId: Guid.Parse(TenantIds.IlbakAccountTenantId));

        // EnsureSystemRoles
        await EnsureRoleAsync(db, logger, systemTenant.Id, DefaultRoleNames.SystemAdmin, true, false, roleId: Guid.Parse(TenantConsts.SystemTenantRoleId));
        await EnsureRoleAsync(db, logger, systemTenant.Id, DefaultRoleNames.SystemUser, true, false);
        await EnsureRoleAsync(db, logger, systemTenant.Id, DefaultRoleNames.RegisteredUser, true, true);

        // EnsureBusinessTenantRoles
        await EnsureRoleAsync(db, logger, techsummusTenant.Id, DefaultRoleNames.TenantAdmin, true, true, roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId));
        await EnsureRoleAsync(db, logger, audioTimeTenant.Id, DefaultRoleNames.TenantAdmin, true, true, roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId));
        await EnsureRoleAsync(db, logger, efaturaPartnerTenant.Id, DefaultRoleNames.TenantAdmin, true, true, roleId: Guid.Parse(TenantRoleIds.EfaturaPartnerTenantRoleId));
        await EnsureRoleAsync(db, logger, ilbakAccountTenant.Id, DefaultRoleNames.TenantAdmin, true, true, roleId: Guid.Parse(TenantRoleIds.IlbakAccountTenantRoleId));

        // EnsureDefaultPasswordPolicies
        await EnsureDefaultPasswordPolicyAsync(db, logger, systemTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, logger, techsummusTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, logger, audioTimeTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, logger, efaturaPartnerTenant.Id);
        await EnsureDefaultPasswordPolicyAsync(db, logger, ilbakAccountTenant.Id);

        // EnsureSystemUsers
        await EnsureUserAsync(db, logger, passwordHasher, systemTenant.Id, DefaultUserNames.SystemAdmin, $"{DefaultUserNames.Admin}@local.dev", DefaultRoleNames.SystemAdmin);
        await EnsureUserAsync(db, logger, passwordHasher, systemTenant.Id, DefaultUserNames.SystemUser, $"{DefaultUserNames.User}@local.dev", DefaultRoleNames.SystemUser);
        await EnsureUserAsync(db, logger, passwordHasher, systemTenant.Id, "hsnsh", "hsnsh@outlook.com", DefaultRoleNames.RegisteredUser);

        // EnsureTenantAdminUsers
        await EnsureUserAsync(db, logger, passwordHasher, techsummusTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{techsummusTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, logger, passwordHasher, audioTimeTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{audioTimeTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, logger, passwordHasher, efaturaPartnerTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{efaturaPartnerTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);
        await EnsureUserAsync(db, logger, passwordHasher, ilbakAccountTenant.Id, DefaultUserNames.TenantAdmin, $"{DefaultUserNames.Admin}@{ilbakAccountTenant.NormalizedName.ToLower()}.com", DefaultRoleNames.TenantAdmin);

        // update changes
        await db.SaveChangesAsync();
    }

    private static async Task<Tenant> GetOrCreateTenantAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        TenantTypes tenantType, string title, string name, Guid? tenantId = null, Guid? parentId = null, string parentPath = null)
    {
        string normalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));

        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

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
            ? tenantType == TenantTypes.System ? "/" : $"/{normalizedName}"
            : $"{parentPath}/{normalizedName}";

        tenant = new Tenant(
            id: tenantId.Value,
            tenantType: tenantType,
            title: title,
            name: name,
            path: normalizedAccessPath,
            parentId: parentId
        );

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED TENANT -> {TenantName} added", nameof(EfCoreSeederService), normalizedName);

        return tenant;
    }

    private static async Task EnsureRoleAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        Guid tenantId, string roleName, bool isStatic, bool isDefault, Guid? roleId = null)
    {
        string normalizedRoleName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(roleName));

        var role = await db.AppRoles.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedName == normalizedRoleName);

        if (role is not null) return;

        roleId ??= Guid.CreateVersion7();

        role = new AppRole(id: roleId.Value, tenantId: tenantId, name: roleName, isDefault: isDefault, isStatic: isStatic);

        db.AppRoles.Add(role);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED ROLE -> {RoleName} added", nameof(EfCoreSeederService), normalizedRoleName);
    }

    private static async Task EnsureUserAsync(IdentityServiceDbContext db, IAppConsoleLogger logger, IPasswordHasher passwordHasher, Guid tenantId, string userName, string email, string roleName)
    {
        string normalizedUserName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(userName));
        string normalizedEmail = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(email));
        string normalizedRoleName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(roleName));

        var user = await db.AppUsers.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedUserName == normalizedUserName);

        if (user is null)
        {
            var userId = tenantId == Guid.Parse(TenantConsts.SystemTenantId) && userName == DefaultUserNames.SystemAdmin
                ? Guid.Parse(TenantConsts.SystemTenantUserId)
                : Guid.CreateVersion7();

            user = new AppUser(
                id: userId,
                tenantId: tenantId,
                userName: userName,
                email: email,
                passwordHash: passwordHasher.Hash(DefaultPassword),
                isStatic: true,
                displayName: null,
                avatarSuffixUrl: null,
                phoneNumber: null,
                languageCode: null
            ) { EmailConfirmed = true };

            db.AppUsers.Add(user);
            await db.SaveChangesAsync();
        }

        var role = await db.AppRoles.FirstAsync(x =>
            x.TenantId == tenantId &&
            x.NormalizedName == normalizedRoleName);

        bool hasRole = await db.AppUserRoles.AnyAsync(x =>
            x.UserId == user.Id &&
            x.RoleId == role.Id);

        if (!hasRole)
        {
            db.AppUserRoles.Add(new AppUserRole(tenantId, user.Id, role.Id));

            await db.SaveChangesAsync();
        }

        logger.LogDebug("{WorkerName} | SEED USER -> {Email} added", nameof(EfCoreSeederService), normalizedEmail);
    }

    private static async Task EnsureDefaultPasswordPolicyAsync(IdentityServiceDbContext db, IAppConsoleLogger logger, Guid tenantId)
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

        logger.LogDebug("{WorkerName} | SEED PASSWORD_POLICY -> {TenantId} added", nameof(EfCoreSeederService), tenantId.ToString());
    }
}