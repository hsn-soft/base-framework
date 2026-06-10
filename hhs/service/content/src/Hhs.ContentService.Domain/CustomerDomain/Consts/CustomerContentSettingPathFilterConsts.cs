using Hhs.ContentService.Domain.CustomerDomain.Entities;

namespace Hhs.ContentService.Domain.CustomerDomain.Consts;

public static class CustomerContentSettingPathFilterConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(CustomerContentSettingPathFilter.PathFilterName);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "ClientPathFilters";
    public const int PathFilterNameMaxLength = 150;
}