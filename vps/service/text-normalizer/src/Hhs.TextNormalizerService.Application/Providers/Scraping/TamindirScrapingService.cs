using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class TamindirScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteer)
    : BaseScrapingService(targetUri, logger, puppeteer)
{
    private const string TitleScript =
        "() => Array.from(document.querySelectorAll('.post-content .post-header .container h1'), (e) => e.innerText)";

    private const string ReleaseTimeScript =
        "() => Array.from(document.querySelectorAll('.post-content .content .post-container .post-detail .post-info .editor-info .post-editor-info time'), (e) => e.getAttribute('datetime'))";

    private const string SpotScript =
        "() => Array.from(document.querySelectorAll('.post-content .post-header .container .post-description'), (e) => e.innerText)";

    private const string DetailsScript =
        "() => Array.from(document.querySelectorAll('.post-content .content .post-container .post-detail .article-area p'), (e) => ({ targetText: e.innerText, filterValue: e.parentNode.className.trim() })).filter(item => item.filterValue === 'article-area')";

    private const string ImageScript =
        "() => Array.from(document.querySelectorAll('.post-content .post-image .container img'), (e) => e.getAttribute('src'))";

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
