using Hhs.AdministrationService.Domain.MenuDomain.Entities;

namespace Hhs.AdministrationService.Domain.MenuDomain.Consts;

public static class AppMenuPermissionConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AppMenuPermission.Id);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "AppMenuPermissions";
}