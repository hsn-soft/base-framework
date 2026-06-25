using Hhs.ContentService.Domain.ContentDomain.Entities;

namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AnalysisContentItemConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AnalysisContentItem.Id);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "AnalysisContentItems";
}
