namespace Hhs.TextNormalizerService.Domain.Settings;

public class TextNormalizerSettings
{
    public bool SkipScrapingOperation { get; set; }
    public bool SkipOutlineOperation { get; set; }
    public bool UseReleaseTimeOldContentOutlineOperation { get; set; }

    public bool UseStructuredOutput { get; set; }

    // When true: analysis-content items are always re-scraped and re-outlined independently,
    // ignoring any previously completed customer-content scraping/outline results.
    // When false (default): reuses existing scraping/outline data from the matched
    // customer-content normalized request if already completed.
    public bool ForceReScrapeAndReOutlineForAnalysis { get; set; }
}