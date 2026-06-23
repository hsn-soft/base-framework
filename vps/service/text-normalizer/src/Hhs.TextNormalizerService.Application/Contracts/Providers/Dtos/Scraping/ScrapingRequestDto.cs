using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;

public sealed class ScrapingRequestDto
{
    [NotNull]
    public string DomainKey { get; set; }

    [NotNull]
    public string Path { get; set; }
}