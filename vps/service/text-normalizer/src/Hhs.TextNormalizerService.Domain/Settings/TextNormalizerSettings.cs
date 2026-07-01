namespace Hhs.TextNormalizerService.Domain.Settings;

public class TextNormalizerSettings
{
    public bool SkipScrapingOperation { get; set; }
    public bool SkipOutlineOperation { get; set; }
    public bool UseReleaseTimeOldContentOutlineOperation { get; set; }

    public bool UseStructuredOutput { get; set; }

    // When true: analysis-content items are always re-scraped, ignoring any previously
    // completed customer-content scraping results. Outline is always re-done regardless.
    public bool ForceReScrapeForAnalysis { get; set; }
}