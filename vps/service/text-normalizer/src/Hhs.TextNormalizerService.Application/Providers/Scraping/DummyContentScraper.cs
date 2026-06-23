namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class DummyContentScraper : IContentScraper
{
    public Task<ScraperResultDto> ScrapeAsync(string url)
    {
        return Task.FromResult(new ScraperResultDto
        {
            Title = $"Dummy title for {url}",
            Text = $"Dummy scraped text for {url}",
            ReleaseTimeUtc = DateTime.UtcNow
        });
    }
}