namespace Hhs.TextNormalizerService.Providers.Scraping;

public sealed class ScraperResultDto
{
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
}

public interface IContentScraper
{
    Task<ScraperResultDto> ScrapeAsync(string url, CancellationToken cancellationToken);
}