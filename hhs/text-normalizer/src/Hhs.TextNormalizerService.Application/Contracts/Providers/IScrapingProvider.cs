using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers;

public interface IScrapingProvider
{
    Task<ScrapingResponseDto> ScrapingAsync(ScrapingRequestDto input);
}