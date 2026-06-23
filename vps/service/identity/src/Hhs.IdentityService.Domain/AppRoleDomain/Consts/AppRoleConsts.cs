using Hhs.IdentityService.Domain.AppRoleDomain.Entities;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Consts;

public static class AppRoleConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AppRole.Name);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "AppRoles";
    public const int NameMaxLength = 256;
}