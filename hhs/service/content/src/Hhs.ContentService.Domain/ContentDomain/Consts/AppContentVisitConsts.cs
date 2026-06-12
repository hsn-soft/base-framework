namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AppContentVisitConsts
{
    private const string DefaultSorting = "{0}VisitTimeLine asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "AppContentVisits";
    public const int ScopeKeyMaxLength = 128;
    public const int VisitResponseMaxLength = 30;
}