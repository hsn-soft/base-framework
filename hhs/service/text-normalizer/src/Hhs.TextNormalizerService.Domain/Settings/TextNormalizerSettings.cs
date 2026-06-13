namespace Hhs.TextNormalizerService.Domain.Settings;

public class TextNormalizerSettings
{
    public bool SkipScrapingOperation { get; set; }
    public bool SkipOutlineOperation { get; set; }
    public bool UseReleaseTimeOldContentOutlineOperation { get; set; }

    public bool UseStructuredOutput { get; set; }
}