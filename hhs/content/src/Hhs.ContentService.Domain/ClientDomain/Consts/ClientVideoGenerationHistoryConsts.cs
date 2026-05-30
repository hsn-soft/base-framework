using Hhs.ContentService.Domain.ClientDomain.Entities;

namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientVideoGenerationHistoryConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(ClientVideoGenerationHistory.VideoGenerationDate);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "ClientVideoGenerationHistories";
    public const int ContentReferenceIdsNameMaxLength = 1000;
}