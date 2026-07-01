using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class CnbceScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteer)
    : BaseScrapingService(targetUri, logger, puppeteer)
{
    private const string TitleScript =
        "() => Array.from(document.querySelectorAll('.infinity-item .post-title'), (e) => e.innerText)";

    private const string ReleaseTimeScript =
        "() => Array.from(document.querySelectorAll('meta[name=\"datePublished\"]'), (e) => e.content)" +
        ".filter((item) => item !== '' && item !== ' ').slice(0, 1)";

    private const string SpotScript = "";

    private const string DetailsScript =
        "() => Array.from(document.querySelectorAll('.infinity-item .content-text'), " +
        "(e) => ({targetText: e.innerText, filterValue: e.parentNode.className.trim()}))" +
        ".filter(item => item.filterValue === 'space-y-5')";

    private const string ImageScript =
        "() => Array.from(document.querySelectorAll('.infinity-item .post-image img'), (e) => e.getAttribute('src'))";

    public override async Task<ScraperResultDto> RunAsync() => await RunScriptAsync(
        titleScript: TitleScript,
        releaseTimeScript: ReleaseTimeScript,
        spotScript: SpotScript,
        detailsScript: DetailsScript,
        imageScript: ImageScript,
        isEnabledCssBlocked: true,
        isEnabledFontBlocked: true,
        isEnabledScriptBlocked: false
    );
}
