using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Setup;

public static class AppRolePermissionAndConstraintSeeder
{
    public static async Task SeedAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger)
    {
        // Set all permission and constraints for system tenant

        var systemTenantRoleId = Guid.Parse(TenantConsts.SystemTenantRoleId);

        var nonConstraintPermissions = await db.Permissions
            .Where(x => x.PermissionType != PermissionTypes.Constraint)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var permissionId in nonConstraintPermissions)
        {
            if (await db.AppRolePermissions.AnyAsync(x => x.AppRoleId == systemTenantRoleId && x.PermissionId == permissionId))
            {
                continue;
            }

            db.AppRolePermissions.Add(new AppRolePermission(
                appRoleId: systemTenantRoleId,
                permissionId: permissionId)
            );

            await db.SaveChangesAsync();
        }


        var constraintPermissions = await db.Permissions
            .Where(x => x.PermissionType == PermissionTypes.Constraint)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var permissionId in constraintPermissions)
        {
            if (await db.AppRolePermissionConstraints.AnyAsync(x => x.AppRoleId == systemTenantRoleId && x.PermissionId == permissionId))
            {
                continue;
            }

            db.AppRolePermissionConstraints.Add(new AppRolePermissionConstraint(
                appRoleId: systemTenantRoleId,
                permissionId: permissionId,
                value: "-1") // no-limit
            );

            await db.SaveChangesAsync();
        }
    }
}