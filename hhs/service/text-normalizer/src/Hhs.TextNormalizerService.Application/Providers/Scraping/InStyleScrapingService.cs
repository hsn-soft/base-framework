using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class InStyleScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeTeer) : BaseScrapingService(targetUri, logger, puppeTeer)
{
    private const string TitleScript = "() => Array.from(document.querySelectorAll('.container .post-title'), (e) => e.innerText)";
    private const string ReleaseTimeScript = "() => Array.from(document.querySelectorAll('meta[property=\"article:published_time\"]'), (e) => { return e.content }) .filter((item) => item !== '' && item !== ' ').slice(0, 1)";
    private const string SpotScript = "";
    private const string DetailsScript = "() => Array.from(document.querySelectorAll('.container .entry-content'), (e) => ({targetText: e.innerText,filterValue: e.parentNode.className.trim()})).filter(item => item.filterValue === 'single56__body')";
    private const string ImageScript = "() => Array.from(document.querySelectorAll('.container .post-thumbnail img'), (e) => e.getAttribute('src'))";

    public override async Task<ScrapingResponseDto> RunAsync() => await RunScriptAsync(
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