using Hhs.ContentService.Domain.ContentDomain.Entities;

namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AnalysisContentConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(AnalysisContent.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "AnalysisContents";
    public const int ScopeKeyMaxLength = 100;
    public const int CorrelationIdMaxLength = 50;
    public const int LastFacilityMaxLength = 120;
    public const int LastErrorMaxLength = 1000;

    public const int NormalizeStatusMaxLength = 80;
    public const int VideoStatusMaxLength = 80;
    public const int FinalVideoUrlMaxLength = 2000;
}