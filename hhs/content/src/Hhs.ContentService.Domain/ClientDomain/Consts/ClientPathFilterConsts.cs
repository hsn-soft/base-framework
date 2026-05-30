using Hhs.ContentService.Domain.ClientDomain.Entities;

namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientPathFilterConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(ClientPathFilter.PathFilterName);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "ClientPathFilters";
    public const int PathFilterNameMaxLength = 150;
}