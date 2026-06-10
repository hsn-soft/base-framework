namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AppContentConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "AppContents";
    public const int SlugKeyMaxLength = 512;
    public const int OperationStatusDescriptionMaxLength = 1024;
    public const int StorageVideoUrlMaxLength = 2048;
    public const int CorrelationIdMaxLength = 128;
}
