namespace Hhs.TextNormalizerService.Domain.ContentDomain.Consts;

public static class NormalizedAnalysisConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "NormalizedAnalysis";
    public const int DomainNameMaxLength = 100;
}