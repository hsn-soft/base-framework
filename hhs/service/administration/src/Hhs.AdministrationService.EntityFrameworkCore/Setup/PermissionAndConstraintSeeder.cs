using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts.Permissions;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Setup;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger)
    {
        // Add service permissions
        var serviceEndpointPermissionUniqueCodeList = AllServiceEndpointPolicyPermissions();
        foreach (string serviceEndpointPermissionUniqueCode in serviceEndpointPermissionUniqueCodeList)
        {
            _ = await GetOrCreatePermissionsAsync(db, logger, serviceEndpointPermissionUniqueCode, PermissionTypes.Service);
        }

        // Add operation access permissions
        var operationPermissionUniqueCodeList = AllOperationAccessPermissions();
        foreach (string operationPermissionUniqueCode in operationPermissionUniqueCodeList)
        {
            _ = await GetOrCreatePermissionsAsync(db, logger, operationPermissionUniqueCode, PermissionTypes.OperationAccess);
        }

        // Add constraint permissions
        var constraintPermissionUniqueCodeList = AllConstraintPermissions();
        foreach (string constraintPermissionUniqueCode in constraintPermissionUniqueCodeList)
        {
            _ = await GetOrCreatePermissionsAsync(db, logger, constraintPermissionUniqueCode, PermissionTypes.Constraint);
        }

        // update changes
        await db.SaveChangesAsync();
    }

    private static async Task<Permission> GetOrCreatePermissionsAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger,
        [NotNull] string uniqueCode,
        PermissionTypes permissionType
    )
    {
        #region Type Control

        switch (permissionType)
        {
            case PermissionTypes.OperationAccess:
            case PermissionTypes.Constraint:
            case PermissionTypes.Service:
                {
                    break;
                }
            case PermissionTypes.Page:
            case PermissionTypes.Unknown:
            default:
                {
                    throw new Exception("Invalid permission type: " + permissionType.ToDisplayName());
                }
        }

        #endregion

        string normalizedUniqueCode = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(uniqueCode));

        if (!normalizedUniqueCode.StartsWith($"{permissionType.ToPrompt()}."))
        {
            throw new Exception("Invalid permission pattern : " + normalizedUniqueCode + " , valid pattern : " + permissionType.ToPrompt() + ".xxx");
        }

        var permissionItem = await db.Permissions.FirstOrDefaultAsync(x => x.UniqueCode == normalizedUniqueCode);

        if (permissionItem is not null)
            return permissionItem;


        permissionItem = new Permission(
            uniqueCode: normalizedUniqueCode,
            permissionType: permissionType,
            name: $"{permissionType.ToDisplayName()} permission"
        );

        db.Permissions.Add(permissionItem);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED {PermissionType} PERMISSION -> {UniqueCode} added"
            , nameof(EfCoreSeederService)
            , permissionType.ToDisplayName()
            , normalizedUniqueCode);

        return permissionItem;
    }

    private static List<string> AllServiceEndpointPolicyPermissions()
    {
        var allPermissions = new List<string>();

        allPermissions.AddRange(AdministrationServicePermissions.GetAll());
        allPermissions.AddRange(IdentityServicePermissions.GetAll());
        allPermissions.AddRange(ContentServicePermissions.GetAll());
        allPermissions.AddRange(TextNormalizerServicePermissions.GetAll());
        allPermissions.AddRange(VideoGeneratorServicePermissions.GetAll());
        allPermissions.AddRange(EventManagerServicePermissions.GetAll());
        allPermissions.AddRange(FeedRAdManagerServicePermissions.GetAll());

        return allPermissions;
    }

    private static List<string> AllOperationAccessPermissions()
    {
        var allPermissions = new List<string>();

        allPermissions.AddRange(AdministrationOperationPermissions.GetAll());
        allPermissions.AddRange(IdentityOperationPermissions.GetAll());
        allPermissions.AddRange(ContentOperationPermissions.GetAll());
        allPermissions.AddRange(TextNormalizerOperationPermissions.GetAll());
        allPermissions.AddRange(VideoGeneratorOperationPermissions.GetAll());
        allPermissions.AddRange(EventManagerOperationPermissions.GetAll());
        allPermissions.AddRange(FeedRAdManagerOperationPermissions.GetAll());
        return allPermissions;
    }

    private static List<string> AllConstraintPermissions()
    {
        var allPermissions = new List<string>();

        allPermissions.AddRange(AdministrationConstraintPermissions.GetAll());
        allPermissions.AddRange(IdentityConstraintPermissions.GetAll());
        allPermissions.AddRange(ContentConstraintPermissions.GetAll());
        allPermissions.AddRange(TextNormalizerConstraintPermissions.GetAll());
        allPermissions.AddRange(VideoGeneratorConstraintPermissions.GetAll());
        allPermissions.AddRange(EventManagerConstraintPermissions.GetAll());
        allPermissions.AddRange(FeedRAdManagerConstraintPermissions.GetAll());

        return allPermissions;
    }
}