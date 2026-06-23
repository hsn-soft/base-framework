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
    public const int ScopeKeyMaxLength = 128;
    public const int OperationStatusDescriptionMaxLength = 1024;
    public const int StorageVideoUrlMaxLength = 2048;
    public const int CorrelationIdMaxLength = 128;
}