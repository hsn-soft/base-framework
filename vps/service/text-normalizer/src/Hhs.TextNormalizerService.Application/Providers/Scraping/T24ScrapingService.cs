using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class T24ScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteer)
    : BaseScrapingService(targetUri, logger, puppeteer)
{
    private const string TitleScript =
        "() => Array.from(document.querySelectorAll('._2hwRY .d0FP9 h1'), (e) => e.innerText)";

    private const string ReleaseTimeScript =
        "() => Array.from(document.querySelectorAll('meta[name=\"datePublished\"]'),(e) => {return e.content}).filter((item) => item !== '' && item !== ' ').slice(0, 1)";

    private const string SpotScript = "";

    private const string DetailsScript =
        "() => Array.from(document.querySelectorAll('._2hwRY ._3QVZl h3,._2hwRY ._3QVZl p'), (e) => ({targetText: e.innerText,filterValue: e.parentNode.parentElement.nodeName.trim()})).filter(item => item.filterValue === 'DIV')";

    private const string ImageScript =
        "() => Array.from(document.querySelectorAll('._2hwRY ._3xXvK'), (e) => e.getAttribute('src'))";

    public override async Task<ScraperResultDto> RunAsync() => await RunScriptAsync(
        titleScript: TitleScript,
        releaseTimeScript: ReleaseTimeScript,
        spotScript: SpotScript,
        detailsScript: DetailsScript,
        imageScript: ImageScript,
        isEnabledCssBlocked: false,
        isEnabledFontBlocked: false,
        isEnabledScriptBlocked: false
    );
}
