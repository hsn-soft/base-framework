using HsnSoft.Base.PuppeTeer;
using PuppeteerSharp;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public sealed class PuppeteerContentScraper(IPuppeteerBrowser puppeteerBrowser) : IContentScraper
{
    public async Task<ScraperResultDto> ScrapeAsync(ScraperRequestDto input)
    {
        var url = $"{input.DomainKey.TrimEnd('/')}{input.Path}";
        var result = new ScraperResultDto();

        try
        {
            await using var lease = await puppeteerBrowser.AcquirePageAsync().ConfigureAwait(false);
            var page = lease.Page;

            var response = await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                Timeout = 30_000
            }).ConfigureAwait(false);

            if (response is not null && !response.Ok)
            {
                result.HasError = true;
                result.Errors.Add($"HTTP {response.Status} from {url}");
                return result;
            }

            result.Title = await page.GetTitleAsync().ConfigureAwait(false);

            result.Details = await page.EvaluateExpressionAsync<string>(
                "document.body?.innerText ?? ''"
            ).ConfigureAwait(false);

            result.Spot = await page.EvaluateExpressionAsync<string>(
                "document.querySelector('meta[name=\"description\"]')?.content ?? ''"
            ).ConfigureAwait(false);

            var publishedTime = await page.EvaluateExpressionAsync<string?>(
                "document.querySelector('meta[property=\"article:published_time\"]')?.content ?? null"
            ).ConfigureAwait(false);

            if (publishedTime is not null && DateTime.TryParse(publishedTime, out var parsed))
                result.ReleaseTimeUtc = parsed.ToUniversalTime();

            result.ImageUrl = await page.EvaluateExpressionAsync<string>(
                "document.querySelector('meta[property=\"og:image\"]')?.content ?? ''"
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.HasError = true;
            result.Errors.Add(ex.Message);
        }

        return result;
    }
}
