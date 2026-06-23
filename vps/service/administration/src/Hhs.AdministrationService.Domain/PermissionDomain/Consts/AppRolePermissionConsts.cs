using Hhs.AdministrationService.Domain.PermissionDomain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Consts;

public static class AppRolePermissionConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AppRolePermission.Id);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "AppRolePermissions";
}