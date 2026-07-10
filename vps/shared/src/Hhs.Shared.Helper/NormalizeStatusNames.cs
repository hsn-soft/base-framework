namespace Hhs.Shared.Helper;


public static class NormalizeStatusNames
{
    public const string NotStarted = "NOT_STARTED";
    public const string Created = "CREATED";

    public const string ScrapingStarted = "SCRAPING_STARTED";
    public const string ScrapingCompleted = "SCRAPING_COMPLETED";

    public const string OutlineStarted = "OUTLINE_STARTED";
    public const string OutlineSkipped = "OUTLINE_SKIPPED";

    public const string OutlineProviderRequestStarted = "OUTLINE_PROVIDER_REQUEST_STARTED";
    public const string OutlineProviderRequestPolling = "OUTLINE_PROVIDER_REQUEST_POLLING";
    public const string OutlineProviderRequestCompleted = "OUTLINE_PROVIDER_REQUEST_COMPLETED";

    public const string OutlineCompleted = "OUTLINE_COMPLETED";

    public const string Completed = "COMPLETED";

    public const string Failed = "FAILED";
    public const string WaitingRetry = "WAITING_RETRY";
}