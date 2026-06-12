using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;

public sealed class ScrapingResponseDto
{
    [CanBeNull]
    public string Title { get; set; }

    public DateTime? ReleaseTime { get; set; }

    [CanBeNull]
    public string Spot { get; set; }

    [CanBeNull]
    public string Details { get; set; }

    [CanBeNull]
    public string ImageUrl { get; set; }

    public bool HasError { get; set; }

    public List<string> Errors { get; set; } = [];
}