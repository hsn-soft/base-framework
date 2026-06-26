namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class DummyContentScraper : IContentScraper
{
    public Task<ScraperResultDto> ScrapeAsync(ScraperRequestDto input)
    {
        return Task.FromResult(new ScraperResultDto
        {
            Title = $"Dummy title for {input.DomainKey}{input.Path}",
            ReleaseTimeUtc = DateTime.UtcNow,
            Spot = "Spot Test",
            Details = $"Dummy scraped text for {input.DomainKey}{input.Path}",
            ImageUrl = ""
        });
    }
}