namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientPathFilterConsts
{
    private const string DefaultSorting = "{0}PathFilterName asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "ClientPathFilters";
    public const int PathFilterNameMaxLength = 150;
}