namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;

public static class AnalysisNormalizedItemConsts
{
    // Content Reference
    public const int ContentKeyMaxLength = 500;

    // Scraping State
    public const int ScrapingStatusMaxLength = 50;

    // Outline Generation State
    public const int OutlineStatusMaxLength = 50;
    public const int OutlineProviderTrackIdMaxLength = 256;

    // Status & Progress
    public const int StatusMaxLength = 80;
    public const int CurrentStepMaxLength = 100;

    // Error Handling & Tracking
    public const int LastErrorMaxLength = 1000;

    // Polling & Retry Configuration
    public const int MaxOutlinePollingCountDefault = 60;
    public const int MaxRetryCountDefault = 5;
}
