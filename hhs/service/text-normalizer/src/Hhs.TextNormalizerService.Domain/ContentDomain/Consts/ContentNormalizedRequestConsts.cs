using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Consts;

public static class ContentNormalizedRequestConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(ContentNormalizedRequest.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = " ContentNormalizedRequests";
    public const int ScopeKeyMaxLength = 128;
    public const int DomainNameMaxLength = 100;
    public const int DomainPathMaxLength = 500;
}