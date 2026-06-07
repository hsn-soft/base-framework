namespace Hhs.ContentService.Domain.DashboardDomain.Consts;

public static class ResponseStatisticConsts
{
    private const string DefaultSorting = "{0}ResponseTime asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "ResponseStatistics";
    public const int ResponseStatusMaxLength = 30;
}