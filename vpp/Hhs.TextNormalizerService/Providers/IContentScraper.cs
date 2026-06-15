namespace Hhs.TextNormalizerService.Providers;

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

public sealed class DummyContentScraper : IContentScraper
{
    public Task<ScraperResultDto> ScrapeAsync(string url, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ScraperResultDto
        {
            Title = $"Dummy title for {url}",
            Text = $"Dummy scraped text for {url}",
            ReleaseTimeUtc = DateTime.UtcNow
        });
    }
}