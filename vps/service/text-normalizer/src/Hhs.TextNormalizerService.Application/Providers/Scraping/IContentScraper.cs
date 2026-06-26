using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class ScraperResultDto
{
    [CanBeNull]
    public string Title { get; set; }

    public DateTime? ReleaseTimeUtc { get; set; }

    [CanBeNull]
    public string Spot { get; set; }

    [CanBeNull]
    public string Details { get; set; }

    [CanBeNull]
    public string ImageUrl { get; set; }

    public bool HasError { get; set; }

    public List<string> Errors { get; set; } = [];
}

public sealed class ScraperRequestDto
{
    [NotNull]
    public string DomainKey { get; set; }

    [NotNull]
    public string Path { get; set; }
}

public interface IContentScraper
{
    Task<ScraperResultDto> ScrapeAsync(ScraperRequestDto input);
}