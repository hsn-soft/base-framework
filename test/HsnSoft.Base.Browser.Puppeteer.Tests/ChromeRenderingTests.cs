using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Browser.Puppeteer.Tests;

/// <summary>The renderer against a real Chrome and a real server on this machine. Where no Chrome is installed (set <c>HSN_TEST_CHROME</c> to one) these tests do nothing.</summary>
public sealed class ChromeRenderingTests : IDisposable
{
    private readonly List<HttpListener> _listeners = [];

    public void Dispose()
    {
        foreach (HttpListener listener in _listeners)
        {
            listener.Close();
        }
    }

    private static string? Chrome()
    {
        string?[] candidates =
        [
            Environment.GetEnvironmentVariable("HSN_TEST_CHROME"),
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/usr/bin/google-chrome",
            "/usr/bin/google-chrome-stable",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        ];
        return candidates.FirstOrDefault(path => !string.IsNullOrEmpty(path) && File.Exists(path));
    }

    private static PuppeteerBrowserRenderer? Renderer(Action<PuppeteerOptions>? configure = null)
    {
        if (Chrome() is not { } chrome)
        {
            return null;
        }

        PuppeteerOptions options = new() { ExecutablePath = chrome, DisableSandbox = true, DisableDevShmUsage = true, AllowPrivateNetworks = true, OperationTimeoutSeconds = 40 };
        configure?.Invoke(options);
        return new PuppeteerBrowserRenderer(Options.Create(options), NullLogger<PuppeteerBrowserRenderer>.Instance);
    }

    /// <summary>A server on this machine; <paramref name="answer"/> writes the answer of each request.</summary>
    private (string Address, List<string> Seen) Serve(Func<HttpListenerContext, Task> answer)
    {
        List<string> seen = [];
        for (int port = 20_000 + Random.Shared.Next(20_000); ; port++)
        {
            HttpListener listener = new();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException)
            {
                listener.Close();
                continue;
            }

            _listeners.Add(listener);
            _ = Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
                        lock (seen)
                        {
                            seen.Add(context.Request.Url!.PathAndQuery);
                        }

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await answer(context);
                            }
                            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                            {
                                // the browser went away
                            }
                        });
                    }
                    catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
                    {
                        return;
                    }
                }
            });
            return ($"http://127.0.0.1:{port}", seen);
        }
    }

    private static async Task Html(HttpListenerContext context, string html, int status = 200)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        context.Response.StatusCode = status;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    [Fact]
    public async Task APage_IsReadAfterItsScriptsRan_WithItsTitleStatusAndPlace()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            (string address, _) = Serve(context => Html(context, "<html><head><title>Hello</title></head><body><p id='x'>before</p><script>document.getElementById('x').textContent = 'after the script';</script></body></html>"));

            RenderedPage page = await renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/page")));

            Assert.Contains("after the script", page.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("before", page.Html, StringComparison.Ordinal);
            Assert.Equal("Hello", page.Title);
            Assert.Equal(200, page.StatusCode);
            Assert.Equal(new Uri($"{address}/page"), page.FinalUrl);
        }
    }

    [Fact]
    public async Task Html_IsRenderedLikeAPage_AndTheViewportIsTheRequests()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            BrowserRequest request = BrowserRequest.FromHtml("<html><head><title>Made</title></head><body><p id='w'></p><script>document.getElementById('w').textContent = window.innerWidth;</script></body></html>") with { ViewportWidth = 777 };

            RenderedPage page = await renderer.GetPageAsync(request);

            Assert.Contains(">777<", page.Html, StringComparison.Ordinal);
            Assert.Equal("Made", page.Title);
            Assert.Null(page.FinalUrl);
            Assert.Null(page.StatusCode);
        }
    }

    [Fact]
    public async Task TheHeadersAndTheUserAgentOfTheRequest_AreSent()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            (string address, _) = Serve(context => Html(context, $"<html><body><p id='ua'>{context.Request.UserAgent}</p><p id='h'>{context.Request.Headers["X-Probe"]}</p></body></html>"));
            BrowserRequest request = BrowserRequest.FromUrl(new Uri($"{address}/echo")) with { UserAgent = "HsnTest/1.0", Headers = new Dictionary<string, string> { ["X-Probe"] = "yes" } };

            RenderedPage page = await renderer.GetPageAsync(request);

            Assert.Contains(">HsnTest/1.0<", page.Html, StringComparison.Ordinal);
            Assert.Contains(">yes<", page.Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task AnElementTheScriptsAdd_CanBeWaitedFor()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            BrowserRequest request = BrowserRequest.FromHtml("<html><body><script>setTimeout(function () { var d = document.createElement('div'); d.id = 'late'; d.textContent = 'arrived'; document.body.appendChild(d); }, 400);</script></body></html>") with { WaitForSelector = "#late" };

            RenderedPage page = await renderer.GetPageAsync(request);

            Assert.Contains("arrived", page.Html, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(WaitCondition.DomContentLoaded)]
    [InlineData(WaitCondition.Load)]
    [InlineData(WaitCondition.NetworkIdle)]
    public async Task EveryWaitCondition_GivesThePage(WaitCondition condition)
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            RenderedPage page = await renderer.GetPageAsync(BrowserRequest.FromHtml("<html><body><p>ready</p></body></html>") with { WaitUntil = condition });

            Assert.Contains("ready", page.Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task APdf_IsMade_OfHtmlAndOfAnAddress_InEveryPaperFormat()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            (string address, _) = Serve(context => Html(context, "<html><body><h1>Invoice</h1></body></html>"));

            byte[] fromHtml = await renderer.RenderPdfAsync(BrowserRequest.FromHtml("<html><body><h1>Invoice</h1></body></html>"));
            byte[] fromUrl = await renderer.RenderPdfAsync(BrowserRequest.FromUrl(new Uri($"{address}/invoice")), new PdfOptions { Landscape = true, PrintBackground = false, MarginMillimeters = 0 });

            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(fromHtml, 0, 5), StringComparison.Ordinal);
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(fromUrl, 0, 5), StringComparison.Ordinal);
            foreach (PaperFormat format in Enum.GetValues<PaperFormat>())
            {
                Assert.True((await renderer.RenderPdfAsync(BrowserRequest.FromHtml("<p>x</p>"), new PdfOptions { Format = format })).Length > 100);
            }
        }
    }

    [Fact]
    public async Task AnImage_IsMade_AsPngOrJpeg_WholePageOrViewport()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            BrowserRequest tall = BrowserRequest.FromHtml("<html><body style='margin:0'><div style='height:3000px;background:#369'></div></body></html>");

            byte[] png = await renderer.ScreenshotAsync(tall);
            byte[] jpeg = await renderer.ScreenshotAsync(tall, new ScreenshotOptions { Format = ImageFormat.Jpeg, Quality = 60 });
            byte[] full = await renderer.ScreenshotAsync(tall, new ScreenshotOptions { FullPage = true });

            Assert.Equal([0x89, 0x50, 0x4E, 0x47], png[..4]);
            Assert.Equal([0xFF, 0xD8], jpeg[..2]);
            Assert.True(full.Length >= png.Length);
        }
    }

    [Fact]
    public async Task WhatThePolicyDoesNotAllow_IsNeverLoaded_AndTheRedirectOfAnAllowedHostIsDecidedToo()
    {
        if (Renderer(options => options.AllowPrivateNetworks = false) is not { } strict)
        {
            return;
        }

        (string secretAddress, List<string> secretSeen) = Serve(context => Html(context, "secret"));
        (string pageAddress, List<string> pageSeen) = Serve(context => Html(context, "page"));
        await using (strict)
        {
            // the policy forbids the inside: the page of this machine is refused before it is opened
            await Assert.ThrowsAsync<BrowserBlockedException>(() => strict.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{pageAddress}/x"))));
            Assert.Empty(pageSeen);
        }

        // an HTML page that asks for the inside is rendered, and the inside is not called
        if (Renderer(options => options.AllowPrivateNetworks = false) is not { } second)
        {
            return;
        }

        await using (second)
        {
            RenderedPage page = await second.GetPageAsync(BrowserRequest.FromHtml($"<html><body><p>mine</p><img src='{secretAddress}/pixel.png'><script src='{secretAddress}/x.js'></script></body></html>"));

            Assert.Contains("mine", page.Html, StringComparison.Ordinal);
            await Task.Delay(300);
            Assert.Empty(secretSeen);
        }

        // an allowed host that redirects to one that is not: the redirect is aborted
        if (Renderer(options =>
        {
            options.AllowPrivateNetworks = true;
            options.AllowedHosts.Add("127.0.0.1");
        }) is not { } third)
        {
            return;
        }

        await using (third)
        {
            (string redirecting, _) = Serve(context =>
            {
                context.Response.StatusCode = 302;
                context.Response.RedirectLocation = "http://169.254.169.254/latest/meta-data";
                context.Response.Close();
                return Task.CompletedTask;
            });

            await Assert.ThrowsAnyAsync<BrowserException>(() => third.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{redirecting}/go"))));
        }
    }

    [Fact]
    public async Task APageThatTakesLongerThanItsTime_IsATimeoutException_AndThePlaceIsFreeAgain()
    {
        if (Renderer() is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            (string address, _) = Serve(async context =>
            {
                if (context.Request.Url!.AbsolutePath == "/slow")
                {
                    await Task.Delay(6000);
                }

                await Html(context, "<html><body>done</body></html>");
            });

            await Assert.ThrowsAsync<TimeoutException>(() => renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/slow")) with { TimeoutSeconds = 1 }));
            RenderedPage fast = await renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/fast")));

            Assert.Contains("done", fast.Html, StringComparison.Ordinal);
            Assert.Equal(0, renderer.ActivePages);
        }
    }

    [Fact]
    public async Task OnlyAsManyPagesAsTheLimit_AreRenderedAtOnce_TheOthersWaitOrTimeOut()
    {
        if (Renderer(options =>
        {
            options.MaxConcurrentPages = 1;
            options.WaitForPageSeconds = 1;
        }) is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            (string address, _) = Serve(async context =>
            {
                await Task.Delay(2500);
                await Html(context, "<html><body>slow</body></html>");
            });
            Task<RenderedPage> first = renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/a")));
            await Task.Delay(500);

            TimeoutException waiting = await Assert.ThrowsAsync<TimeoutException>(() => renderer.GetPageAsync(BrowserRequest.FromHtml("<p>second</p>")));

            Assert.Contains("No place", waiting.Message, StringComparison.Ordinal);
            Assert.Contains("slow", (await first).Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task TheBrowserIsReplaced_AfterTheNumberOfPagesOfTheSettings()
    {
        if (Renderer(options => options.RecycleAfterPages = 2) is not { } renderer)
        {
            return;
        }

        await using (renderer)
        {
            for (int page = 0; page < 5; page++)
            {
                RenderedPage rendered = await renderer.GetPageAsync(BrowserRequest.FromHtml($"<html><body>page {page}</body></html>"));
                Assert.Contains($"page {page}", rendered.Html, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task ADisposedRenderer_WaitsForThePagesBeingRendered_AndTheyFinish()
    {
        if (Renderer(options => options.ShutdownTimeoutSeconds = 10) is not { } renderer)
        {
            return;
        }

        (string address, _) = Serve(async context =>
        {
            await Task.Delay(1500);
            await Html(context, "<html><body>finished</body></html>");
        });
        Task<RenderedPage> running = renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/a")));
        await Task.Delay(700);

        await renderer.DisposeAsync();

        Assert.Equal(0, renderer.ActivePages);
        Assert.Contains("finished", (await running).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADisposedRendererThatCannotWait_ClosesTheBrowserAnyway()
    {
        if (Renderer(options => options.ShutdownTimeoutSeconds = 0) is not { } renderer)
        {
            return;
        }

        (string address, _) = Serve(async context =>
        {
            await Task.Delay(4000);
            await Html(context, "x");
        });
        Task<RenderedPage> running = renderer.GetPageAsync(BrowserRequest.FromUrl(new Uri($"{address}/a")));
        await Task.Delay(700);

        await renderer.DisposeAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => running);
    }
}
