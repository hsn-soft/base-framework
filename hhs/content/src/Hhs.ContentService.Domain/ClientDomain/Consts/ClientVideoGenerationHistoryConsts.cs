namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientVideoGenerationHistoryConsts
{
    private const string DefaultSorting = "{0}VideoGenerationDate asc";
 
    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }
 
    public const string TableName = "ClientVideoGenerationHistories";
}