using Hhs.AdministrationService.Domain.PermissionDomain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Consts;

public static class PermissionConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(Permission.UniqueCode);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "Permissions";
    public const int UniqueCodeMaxLength = 300;
    public const int NameMaxLength = 300;
    public const int DescriptionMaxLength = 1000;
}