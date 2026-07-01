using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class PuppeteerContentScraper(IAppConsoleLogger logger, IPuppeteerBrowser puppeteerBrowser) : IContentScraper
{
    public async Task<ScraperResultDto> ScrapeAsync(ScraperRequestDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.DomainKey) || string.IsNullOrWhiteSpace(input.Path))
            throw new InvalidOperationException("INVALID_SCRAPING_REQUEST");

        if (input.DomainKey == "localhost")
        {
            return new ScraperResultDto
            {
                Title = "Title Test",
                ReleaseTimeUtc = DateTime.UtcNow,
                Spot = "Spot Test",
                Details = "Test Details",
                ImageUrl = "Image Test"
            };
        }

        var targetUri = new Uri($"https://{input.DomainKey.TrimStart('/')}{input.Path}");

        BaseScrapingService service = input.DomainKey switch
        {
            "demo.techsummus.com"    => new TechSummusDemoScrapingService(targetUri, logger, puppeteerBrowser),
            "www.tamindir.com"       => new TamindirScrapingService(targetUri, logger, puppeteerBrowser),
            "www.t24.com.tr"         => new T24ScrapingService(targetUri, logger, puppeteerBrowser),
            "www.cnbce.com"          => new CnbceScrapingService(targetUri, logger, puppeteerBrowser),
            "www.diyetkolik.com"     => new DiyetkolikScrapingService(targetUri, logger, puppeteerBrowser),
            "www.instyle.com.tr"     => new InStyleScrapingService(targetUri, logger, puppeteerBrowser),
            "www.boxofficeturkiye.com" => throw new NotSupportedException("DOMAIN_SCRAPER_NOT_CONFIGURED: www.boxofficeturkiye.com"),
            _                        => throw new NotSupportedException($"INVALID_SCRAPING_DOMAIN: {input.DomainKey}")
        };

        return await service.RunAsync();
    }
}
