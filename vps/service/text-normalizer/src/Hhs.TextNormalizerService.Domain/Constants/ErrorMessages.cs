namespace Hhs.TextNormalizerService.Domain.Constants;

public static class ErrorMessages
{
    public const string ProviderKeyValueUnknown = "Provider key value is unknown. Scope key:";

    public const string OutlineProviderPollingTimeout = "Outline provider polling timeout.";
    public const string OutlineProviderFailed = "Outline provider failed.";
    public const string OutlineProviderFailedEmptyScript = "Outline provider completed but script is empty.";

    public const string ScrapingDataNotFound = "SCRAPING DATA NOT FOUND";
    public const string ScrapingDataEmpty = "SCRAPING DATA IS EMPTY";
    public const string ScrapingResultRequired = "ScrapingResult is required.";
    public const string OutlineScrapingDataUnknown = "OUTLINE SCRAPING DATA UNKNOWN";
    public const string OutlineResponseDataUnknown = "OUTLINE RESPONSE CONTENT DATA UNKNOWN";
    public const string OutlineProviderTrackIdRequiredAsync = "ProviderTrackId is required for async outline provider.";
    public const string RefContentTypeRequired = "RefContentType is required.";
    public const string CustomerContentIdForItemRequired = "CustomerContentIdForItem is required.";
    public const string CustomerContentIdForItemRequiredForOutlinePolling = "CustomerContentIdForItem is required for analysis outline polling.";
    public const string CustomerContentIdForItemRequiredForOutlineCompletion = "CustomerContentIdForItem is required for analysis outline completion.";
}
