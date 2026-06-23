using Hhs.ContentService.Domain.ContentDomain.Entities;

namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class ContentVideoGenerationLimitConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(ContentVideoGenerationLimit.VideoGenerationDate);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "ContentVideoGenerationLimits";
    public const int ScopeKeyMaxLength = 128;
    public const int ContentReferenceIdsNameMaxLength = 1000;
}