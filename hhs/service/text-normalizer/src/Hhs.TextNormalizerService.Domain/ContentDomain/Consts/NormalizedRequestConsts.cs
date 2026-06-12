namespace Hhs.TextNormalizerService.Domain.ContentDomain.Consts;

public static class NormalizedRequestConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "NormalizedRequests";
    public const int DomainNameMaxLength = 100;
    public const int DomainPathMaxLength = 500;
}