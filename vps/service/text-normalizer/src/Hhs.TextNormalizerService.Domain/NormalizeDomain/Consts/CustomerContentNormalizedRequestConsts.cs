using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;

public static class CustomerContentNormalizedRequestConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(CustomerContentNormalizedRequest.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "CustomerContentNormalizedRequests";
    public const int ScopeKeyMaxLength = 100;
    public const int CorrelationIdMaxLength = 50;
    public const int DomainNameMaxLength = 500;
    public const int ContentKeyMaxLength = 500;
    public const int StatusMaxLength = 80;
    public const int CurrentStepMaxLength = 100;
    public const int ScrapingStatusMaxLength = 50;
    public const int OutlineStatusMaxLength = 50;
    public const int OutlineProviderTrackIdMaxLength = 256;
    public const int LastErrorMaxLength = 1000;
}
