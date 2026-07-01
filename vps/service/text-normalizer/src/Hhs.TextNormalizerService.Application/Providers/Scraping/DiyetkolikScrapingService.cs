using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class DiyetkolikScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteer)
    : BaseScrapingService(targetUri, logger, puppeteer)
{
    private const string TitleScript =
        "() => Array.from(document.querySelectorAll('.DetailLayout .page-head-title'), (e) => e.innerText)";

    private const string ReleaseTimeScript =
        "() => Array.from(document.querySelectorAll('.DetailLayout .Information span'), (e) => ({targetText: e.innerText,filterValue: e.parentNode.className.trim()})).filter(item => item.targetText !== '').slice(0,1).map(item => item.targetText)";

    private const string SpotScript =
        "() => Array.from(document.querySelectorAll('.DetailLayout .page-head-summary'), (e) => e.innerText)";

    private const string DetailsScript =
        "() => Array.from(document.querySelectorAll('.DetailLayout .icerik'), (e) => ({targetText: e.innerText,filterValue: e.parentNode.className.trim()})).filter(item => item.targetText !== '')";

    private const string ImageScript =
        "() => Array.from(document.querySelectorAll('.DetailLayout .object-fit-cover'), (e) => e.getAttribute('src'))";

    public override async Task<ScraperResultDto> RunAsync() => await RunScriptAsync(
        titleScript: TitleScript,
        releaseTimeScript: ReleaseTimeScript,
        spotScript: SpotScript,
        detailsScript: DetailsScript,
        imageScript: ImageScript,
        isEnabledCssBlocked: true,
        isEnabledFontBlocked: true,
        isEnabledScriptBlocked: true
    );
}
