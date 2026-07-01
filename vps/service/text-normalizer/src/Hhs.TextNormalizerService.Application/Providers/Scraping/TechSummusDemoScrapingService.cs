using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class TechSummusDemoScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteer)
    : BaseScrapingService(targetUri, logger, puppeteer)
{
    private const string TitleScript =
        "() => Array.from(document.querySelectorAll('.spnc-blog-wrapper .spnc-post .spnc-post-content .entry-header h1'), (e) => e.innerText)";

    private const string ReleaseTimeScript =
        "() => Array.from(document.querySelectorAll('.spnc-blog-wrapper .spnc-post .spnc-post-content .spnc-entry-meta .spnc-date time'), (e) => e.getAttribute('itemprop')).slice(0,1)";

    private const string SpotScript =
        "() => Array.from(document.querySelectorAll('.spnc-blog-wrapper .spnc-post .spnc-post-content .entry-header h1'), (e) => e.innerText)";

    private const string DetailsScript =
        "() => Array.from(document.querySelectorAll('.spnc-blog-wrapper .spnc-post .spnc-post-content .spnc-entry-content p,.spnc-blog-wrapper .spnc-post .spnc-post-content .spnc-entry-content li'), (e) => ({ targetText: e.innerText, filterValue: 'serdar' })).filter(item => item.filterValue === 'serdar')";

    private const string ImageScript = "";

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
