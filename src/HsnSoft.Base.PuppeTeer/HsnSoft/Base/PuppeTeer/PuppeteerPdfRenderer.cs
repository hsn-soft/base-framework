using System;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerPdfRenderer
{
    private readonly IPuppeteerBrowser _puppeteerBrowser;

    public PuppeteerPdfRenderer(IPuppeteerBrowser puppeteerBrowser) { _puppeteerBrowser = puppeteerBrowser ?? throw new ArgumentNullException(nameof(puppeteerBrowser)); }

    public async Task<byte[]> RenderPdfFromHtmlAsync(
        string html,
        PdfOptions pdfOptions,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await _puppeteerBrowser.AcquirePageAsync(
            async (page, _) =>
            {
                await page.SetCacheEnabledAsync(false).ConfigureAwait(false);
                await page.EmulateMediaTypeAsync(MediaType.Screen).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        var targetPage = lease.Page;

        await targetPage.SetContentAsync(
            html,
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                Timeout = 30000
            }).ConfigureAwait(false);

        await targetPage.WaitForFunctionAsync(
            "() => document.readyState === 'interactive' || document.readyState === 'complete'",
            new WaitForFunctionOptions { Timeout = 5000 }).ConfigureAwait(false);

        await targetPage.WaitForFunctionAsync(
            "() => document.fonts ? document.fonts.status === 'loaded' : true",
            new WaitForFunctionOptions { Timeout = 5000 }).ConfigureAwait(false);

        return await targetPage.PdfDataAsync(pdfOptions).ConfigureAwait(false);
    }
}