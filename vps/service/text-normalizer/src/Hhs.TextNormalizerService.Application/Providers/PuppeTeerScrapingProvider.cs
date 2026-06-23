using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers;

public sealed class PuppeTeerScrapingProvider(IAppConsoleLogger logger, IPuppeteerBrowser puppeTeer) : IScrapingProvider
{
    public async Task<ScrapingResponseDto> ScrapingAsync(ScrapingRequestDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.DomainKey) || string.IsNullOrWhiteSpace(input.Path))
        {
            throw new Exception("INVALID_SCRAPING_REQUEST");
        }

        var scrapingUri = new Uri($"http://{input.DomainKey}{input.Path}");
        return input.DomainKey switch
        {
            "demo.techsummus.com" => await new TechSummusDemoScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.tamindir.com" => await new TamindirScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.t24.com.tr" => await new T24ScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.cnbce.com" => await new CnbceScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.diyetkolik.com" => await new DiyetkolikScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.instyle.com.tr" => await new InStyleScrapingService(scrapingUri, logger, puppeTeer).RunAsync(),
            "www.boxofficeturkiye.com" => throw new Exception("NOT_IMPLEMENTED_YET"),
            "localhost" => new ScrapingResponseDto
            {
                Title = "Title Test",
                ReleaseTime = DateTime.UtcNow,
                Spot = "Spot Test",
                Details = "Test Details",
                ImageUrl = "Image Test"
            },
            _ => throw new Exception("INVALID_SCRAPING_DOMAIN")
        };
    }
}