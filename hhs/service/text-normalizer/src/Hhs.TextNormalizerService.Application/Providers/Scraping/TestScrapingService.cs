using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class TestScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeTeer) : BaseScrapingService(targetUri, logger, puppeTeer)
{
    private const string TitleScript = "() => Array.from(document.querySelectorAll('.post-content .post-header .container h1'), (e) => e.innerText)";
    private const string ReleaseTimeScript = "() => Array.from(document.querySelectorAll('.post-content .content .post-container .post-detail .post-info .editor-info .post-editor-info time'), (e) => e.getAttribute('datetime'))";
    private const string SpotScript = "() => Array.from(document.querySelectorAll('.post-content .post-header .container .post-description'), (e) => e.innerText)";
    private const string DetailsScript = "() => Array.from(document.querySelectorAll('.post-content .content .post-container .post-detail .article-area p'), (e) => ({ targetText: e.innerText, filterValue: e.parentNode.className.trim() })).filter(item => item.filterValue === 'article-area')";
    private const string ImageScript = "() => Array.from(document.querySelectorAll('.post-content .content .post-container .post-detail .article-area p'), (e) => ({ targetText: e.innerText, filterValue: e.parentNode.className.trim() })).filter(item => item.filterValue === 'article-area')";

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

    protected override Task<ScrapingResponseDto> RunScriptAsync(string titleScript, string releaseTimeScript, string spotScript, string detailsScript, string imageScript, bool isEnabledFontBlocked = true, bool isEnabledCssBlocked = true, bool isEnabledScriptBlocked = true, CancellationToken cancellationToken = default)
    {
        // OVERRIDE BASE SCRIPT FUNCTION
        Logger.LogInformation(titleScript);

        return base.RunScriptAsync(titleScript, releaseTimeScript, spotScript, detailsScript, imageScript, isEnabledFontBlocked, isEnabledCssBlocked, isEnabledScriptBlocked, cancellationToken);
    }

    public async Task<List<SampleModel>> TestAsync()
    {
        //var page = (await browser.PagesAsync()).First();
        await using var page = await (await PuppeTeerService.GetBrowserSafelyAsync()).NewPageAsync();

        #region FileOperations

        // await page.GoToAsync("http://www.google.com");
        // if (!Directory.Exists("Files")) Directory.CreateDirectory("Files");
        // if (!Directory.Exists("Files/Images")) Directory.CreateDirectory("Files/Images");
        // await page.ScreenshotAsync("Files/Images/sample.png");

        #endregion

        #region Propery

        // await page.GoToAsync("https://www.hardkoded.com/blog/ui-testing-with-puppeteer-released");
        // var pageHeaderHandle = await page.QuerySelectorAsync("h1");
        // var innerTextHandle = await pageHeaderHandle.GetPropertyAsync("innerText");
        // var innerText = await innerTextHandle.JsonValueAsync();
        // return innerText.ToString();

        #endregion

        #region OnlyHtmlResponse

        // await page.SetRequestInterceptionAsync(true);
        // page.Request += (async (sender, e) =>
        // {
        //     if (e.Request is not { ResourceType : ResourceType.Document or ResourceType.Xhr or ResourceType.Script or ResourceType.Fetch })
        //     {
        //         // Abort request
        //         await e.Request.AbortAsync();
        //     }
        //     else
        //     {
        //         // Forward the intercepted request
        //         await e.Request.ContinueAsync();
        //     }
        // });
        // // page.Request -= RequestEventListener;
        // await page.GoToAsync("https://www.hardkoded.com", new NavigationOptions
        // {
        //     Timeout = 5000,
        //     WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
        // });
        //
        // var res = await page.QuerySelectorAsync(".page-subheading")
        //     .EvaluateFunctionAsync<string>("el => el.innerText");
        //
        // return res;

        #endregion

        await page.GoToAsync("https://www.traversymedia.com", new NavigationOptions { Timeout = 5000, WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });

        string html = await page.GetContentAsync();
        Logger.LogInformation(html);

        await page.WaitForSelectorAsync("#cscourses");
        int seven = await page.EvaluateExpressionAsync<int>("4 + 3");
        dynamic someObject = await page.EvaluateFunctionAsync<dynamic>("(value) => ({a: value})", 5);
        dynamic title = await page.EvaluateFunctionAsync<dynamic>("()=> document.title");
        Logger.LogInformation(title);

        dynamic text = await page.EvaluateFunctionAsync<dynamic>("()=> document.body.innerText");
        Logger.LogInformation(text);

        dynamic links = await page.EvaluateFunctionAsync<dynamic>("() => Array.from(document.querySelectorAll('a'), (e) => e.href)");
        Logger.LogInformation(links.ToString());

        dynamic courses = await page.EvaluateFunctionAsync<dynamic>(
            "() => Array.from(document.querySelectorAll('#cscourses .card'), (e) => ({title: e.querySelector('.card-body h3').innerText,level: e.querySelector('.card-body .level').innerText,url: e.querySelector('.card-footer a').href}))");
        // var courses = await page.evaluate(() =>
        //     Array.from(document.querySelectorAll('#cscourses .card'), (e) => ({
        //         title: e.querySelector('.card-body h3').innerText,
        //         level: e.querySelector('.card-body .level').innerText,
        //         url: e.querySelector('.card-footer a').href
        //     }))
        // );

        List<dynamic> failedEnvelope = JsonConvert.DeserializeObject<List<dynamic>>(courses.ToString());
        var modelList = failedEnvelope.Select(item => ((JObject)item)?.ToObject<SampleModel>()).ToList();

        return modelList;
    }
}

public class SampleModel
{
    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("level")] public string Level { get; set; }

    [JsonProperty("url")] public string Url { get; set; }
}