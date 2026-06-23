using Hhs.AdministrationService.Domain.MenuDomain.Entities;

namespace Hhs.AdministrationService.Domain.MenuDomain.Consts;

public static class AppMenuConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AppMenu.UniqueCode);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "AppMenus";
    public const int UniqueCodeMaxLength = 200;
    public const int TitleMaxLength = 200;
    public const int RouteMaxLength = 500;
    public const int IconMaxLength = 100;
}