namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;

public static class CustomerContentNormalizedRequestConsts
{
    public const string CollectionName = "customer_content_normalized_requests";
    public const int ScopeKeyMaxLength = 100;
    public const int DomainNameMaxLength = 500;
    public const int ContentKeyMaxLength = 500;
    public const int CurrentStepMaxLength = 100;
    public const int ScrapingStatusMaxLength = 50;
    public const int OutlineStatusMaxLength = 50;
    public const int OutlineProviderTrackIdMaxLength = 256;

    // Polling & Retry Configuration
    public const int MaxOutlinePollingCountDefault = 60;
    public const int MaxRetryCountDefault = 5;
}
