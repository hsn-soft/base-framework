namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AnalysisContentConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";
 
    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }
 
    public const string TableName = "AnalysisContents";
}