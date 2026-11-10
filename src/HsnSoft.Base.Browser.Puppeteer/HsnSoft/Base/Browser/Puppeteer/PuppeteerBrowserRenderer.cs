using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace HsnSoft.Base.Browser.Puppeteer;

/// <summary>
/// <see cref="IBrowserRenderer"/> on Chrome. The browser is started when the first page is needed (not when the host starts) and again when it is gone or has rendered <see cref="PuppeteerOptions.RecycleAfterPages"/> pages; no more than
/// <see cref="PuppeteerOptions.MaxConcurrentPages"/> pages are open at once; every call has a time of its own and a page is always closed. Every request of a page goes through <see cref="BrowserUrlPolicy"/> (an address that is not allowed is
/// aborted), and the address of the page itself is checked before the page is opened. A host that stops waits for the pages that are being rendered (<see cref="PuppeteerOptions.ShutdownTimeoutSeconds"/>) and then closes Chrome.
/// </summary>
public sealed partial class PuppeteerBrowserRenderer : IBrowserRenderer, IAsyncDisposable
{
    // what a driven Chrome gives away, set as a person's Chrome has it (the script of the previous system, which the sites it read answered)
    private const string StealthScript = """
        () => {
            Object.defineProperty(navigator, 'webdriver', { get: () => false });
            Object.defineProperty(navigator, 'languages', { get: () => ['tr-TR', 'tr'] });
            window.chrome = { runtime: {} };
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) =>
                parameters.name === 'notifications'
                    ? Promise.resolve({ state: Notification.permission })
                    : originalQuery(parameters);
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4] });
            Object.defineProperty(navigator, 'mimeTypes', { get: () => [1, 2, 3] });
            Object.defineProperty(navigator, 'hardwareConcurrency', { get: () => 8 });
            Object.defineProperty(navigator, 'deviceMemory', { get: () => 8 });
            Object.defineProperty(navigator, 'platform', { get: () => 'Win32' });
            Object.defineProperty(navigator, 'maxTouchPoints', { get: () => 0 });
            const getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                if (parameter === 37445) return 'Intel Inc.';
                if (parameter === 37446) return 'Intel(R) HD Graphics 630';
                return getParameter.call(this, parameter);
            };
        }
        """;

    private readonly PuppeteerOptions _options;
    private readonly BrowserUrlPolicy _policy;
    private readonly ILogger<PuppeteerBrowserRenderer> _logger;
    private readonly SemaphoreSlim _slots;
    private readonly SemaphoreSlim _launch = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private IBrowser? _browser;
    private int _rendered;
    private int _active;
    private int _disposed;

    public PuppeteerBrowserRenderer(IOptions<PuppeteerOptions> options, ILogger<PuppeteerBrowserRenderer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _options.Validate();
        _logger = logger;
        _slots = new SemaphoreSlim(_options.MaxConcurrentPages, _options.MaxConcurrentPages);
        BrowserUrlPolicyOptions policy = new() { AllowPrivateNetworks = _options.AllowPrivateNetworks };
        foreach (string host in _options.AllowedHosts)
        {
            policy.AllowedHosts.Add(host);
        }

        _policy = new BrowserUrlPolicy(policy);
    }

    /// <summary>The number of pages being rendered now.</summary>
    public int ActivePages => Volatile.Read(ref _active);

    public Task<RenderedPage> GetPageAsync(BrowserRequest request, CancellationToken cancellationToken = default)
        => RunAsync(request, async (page, response) =>
        {
            string html = await page.GetContentAsync().ConfigureAwait(false);
            Uri? finalUrl = request.Url is null ? null : new Uri(page.Url);
            Dictionary<string, ScriptResult>? scripts = request.Scripts is { Count: > 0 } ? await RunScriptsAsync(page, request.Scripts).ConfigureAwait(false) : null;
            return new RenderedPage(html, finalUrl, response is null ? null : (int)response.Status, await page.GetTitleAsync().ConfigureAwait(false), scripts);
        }, cancellationToken);

    public Task<byte[]> RenderPdfAsync(BrowserRequest request, Base.Browser.PdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        Base.Browser.PdfOptions pdf = options ?? new Base.Browser.PdfOptions();
        PuppeteerSharp.Media.PaperFormat format = pdf.Format switch
        {
            Base.Browser.PaperFormat.A3 => PuppeteerSharp.Media.PaperFormat.A3,
            Base.Browser.PaperFormat.A5 => PuppeteerSharp.Media.PaperFormat.A5,
            Base.Browser.PaperFormat.Letter => PuppeteerSharp.Media.PaperFormat.Letter,
            Base.Browser.PaperFormat.Legal => PuppeteerSharp.Media.PaperFormat.Legal,
            _ => PuppeteerSharp.Media.PaperFormat.A4,
        };
        string margin = $"{pdf.MarginMillimeters}mm";
        return RunAsync(request, async (page, _) => await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
        {
            Format = format,
            Landscape = pdf.Landscape,
            PrintBackground = pdf.PrintBackground,
            MarginOptions = new MarginOptions { Top = margin, Bottom = margin, Left = margin, Right = margin },
        }).ConfigureAwait(false), cancellationToken);
    }

    public Task<byte[]> ScreenshotAsync(BrowserRequest request, Base.Browser.ScreenshotOptions? options = null, CancellationToken cancellationToken = default)
    {
        Base.Browser.ScreenshotOptions shot = options ?? new Base.Browser.ScreenshotOptions();
        if (shot.Quality is < 1 or > 100)
        {
            throw new ArgumentException("The quality has to be between 1 and 100.", nameof(options));
        }

        return RunAsync(request, async (page, _) => await page.ScreenshotDataAsync(new PuppeteerSharp.ScreenshotOptions
        {
            Type = shot.Format == Base.Browser.ImageFormat.Jpeg ? ScreenshotType.Jpeg : ScreenshotType.Png,
            Quality = shot.Format == Base.Browser.ImageFormat.Jpeg ? shot.Quality : null,
            FullPage = shot.FullPage,
        }).ConfigureAwait(false), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        // no new call is taken from here on (the flag above); the pages being rendered get the time of the settings to finish, and then they are ended
        DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(_options.ShutdownTimeoutSeconds);
        while (ActivePages > 0 && DateTimeOffset.UtcNow < until)
        {
            await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);   // not the token of the shutdown: it is cancelled already, and the pages are given the time of the settings
        }

        if (ActivePages > 0)
        {
            LogDrainTimeout(_logger, ActivePages);
        }

        await _stopping.CancelAsync().ConfigureAwait(false);

        IBrowser? browser = Interlocked.Exchange(ref _browser, null);
        if (browser is not null)
        {
            await CloseAsync(browser).ConfigureAwait(false);
        }

        _stopping.Dispose();
        _slots.Dispose();
        _launch.Dispose();
    }

    private async Task<T> RunAsync<T>(BrowserRequest request, Func<IPage, IResponse?, Task<T>> take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        if (request.Url is not null && !await _policy.AllowsAsync(request.Url.AbsoluteUri, cancellationToken).ConfigureAwait(false))
        {
            throw new BrowserBlockedException($"The policy does not allow '{request.Url.Host}'.");
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopping.Token);
        linked.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds ?? _options.OperationTimeoutSeconds));
        bool placed = false;
        try
        {
            if (!await _slots.WaitAsync(TimeSpan.FromSeconds(_options.WaitForPageSeconds), linked.Token).ConfigureAwait(false))
            {
                throw new TimeoutException($"No place among the {_options.MaxConcurrentPages} pages was free for {_options.WaitForPageSeconds} seconds.");
            }

            placed = true;
            Interlocked.Increment(ref _active);
            IBrowser browser = await BrowserAsync(linked.Token).ConfigureAwait(false);
            IPage page = await browser.NewPageAsync().ConfigureAwait(false);
            try
            {
                return await RenderAsync(page, request, take, linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
            }
            finally
            {
                await GuardedAsync(() => page.CloseAsync()).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_stopping.IsCancellationRequested)
        {
            // the time of the call ended, in the queue, in the start of Chrome or in the page
            throw new TimeoutException("The call took longer than its time.");
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or TimeoutException or ArgumentException or ObjectDisposedException or BrowserException))
        {
            throw new BrowserException($"The page could not be rendered: {exception.Message}", exception);
        }
        finally
        {
            if (placed)
            {
                Interlocked.Decrement(ref _active);
                _slots.Release();
            }
        }
    }

    private async Task<T> RenderAsync<T>(IPage page, BrowserRequest request, Func<IPage, IResponse?, Task<T>> take, CancellationToken cancellationToken)
    {
        page.DefaultTimeout = (int)TimeSpan.FromSeconds(_options.NavigationTimeoutSeconds).TotalMilliseconds;
        await page.SetViewportAsync(new ViewPortOptions { Width = request.ViewportWidth, Height = request.ViewportHeight }).ConfigureAwait(false);
        if (request.UserAgent is not null)
        {
            await page.SetUserAgentAsync(request.UserAgent).ConfigureAwait(false);
        }

        if (request.Headers is { Count: > 0 })
        {
            await page.SetExtraHttpHeadersAsync(request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value)).ConfigureAwait(false);
        }

        if (request.DisableCache)
        {
            await page.SetCacheEnabledAsync(false).ConfigureAwait(false);
        }

        if (request.Stealth)
        {
            await page.EvaluateFunctionOnNewDocumentAsync(StealthScript).ConfigureAwait(false);
        }

        if (_options.ProxyServer is { Length: > 0 } && !string.IsNullOrWhiteSpace(_options.ProxyUserName))
        {
            await page.AuthenticateAsync(new Credentials { Username = _options.ProxyUserName, Password = _options.ProxyPassword ?? string.Empty }).ConfigureAwait(false);
        }

        // every request of the page, the redirects and what the scripts fetch included, is decided by the policy and by what the request leaves unloaded
        await page.SetRequestInterceptionAsync(true).ConfigureAwait(false);
        BrowserResources blocked = request.BlockedResources;
        page.Request += (_, args) => _ = DecideAsync(args.Request, blocked, cancellationToken);

        NavigationOptions navigation = new() { WaitUntil = [WaitFor(request.WaitUntil)], Timeout = (int)TimeSpan.FromSeconds(_options.NavigationTimeoutSeconds).TotalMilliseconds };
        IResponse? response = null;
        if (request.Url is not null)
        {
            response = await page.GoToAsync(request.Url.AbsoluteUri, navigation).ConfigureAwait(false);
        }
        else
        {
            // SetContent does not support the network-idle conditions (PuppeteerSharp ignores them, and upstream never made them work); the load event comes after DOMContentLoaded, so it
            // stands for both of the page conditions, and the network idle is waited for apart, as the library says (it hung for the whole time of the call on a slow CI runner).
            await page.SetContentAsync(request.Html!, new SetContentOptions { WaitUntil = [WaitUntilNavigation.Load], Timeout = navigation.Timeout }).ConfigureAwait(false);
            if (request.WaitUntil == WaitCondition.NetworkIdle)
            {
                await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = navigation.Timeout }).ConfigureAwait(false);
            }
        }

        if (request.WaitForSelector is not null)
        {
            await page.WaitForSelectorAsync(request.WaitForSelector, new WaitForSelectorOptions { Timeout = (int)TimeSpan.FromSeconds(_options.NavigationTimeoutSeconds).TotalMilliseconds }).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await take(page, response).ConfigureAwait(false);
    }

    // the kind of a request that is left unloaded when the call says so: never the document of the navigation
    private static bool IsLeftOut(IRequest request, BrowserResources blocked)
    {
        if (blocked == BrowserResources.None || (request.IsNavigationRequest && request.ResourceType == ResourceType.Document))
        {
            return false;
        }

        BrowserResources kind = request.ResourceType switch
        {
            ResourceType.Media => BrowserResources.Media,
            ResourceType.Font => BrowserResources.Fonts,
            ResourceType.StyleSheet => BrowserResources.Stylesheets,
            ResourceType.Script => BrowserResources.Scripts,
            ResourceType.Image => BrowserResources.Images,
            _ => BrowserResources.None,
        };
        return (blocked & kind) != BrowserResources.None;
    }

    // the functions of a request run in the page, in order, each with its own result: one that fails says why and the others still run
    private static async Task<Dictionary<string, ScriptResult>> RunScriptsAsync(IPage page, IReadOnlyDictionary<string, string> scripts)
    {
        Dictionary<string, ScriptResult> results = [];
        foreach ((string name, string source) in scripts)
        {
            try
            {
                JsonElement value = await page.EvaluateFunctionAsync<JsonElement>(source).ConfigureAwait(false);
                results[name] = new ScriptResult(value.ValueKind == JsonValueKind.Undefined ? null : value, null);
            }
            catch (Exception exception) when (exception is PuppeteerException or JsonException or InvalidOperationException)
            {
                results[name] = new ScriptResult(null, exception.Message);
            }
        }

        return results;
    }

    private Task DecideAsync(IRequest request, BrowserResources blocked, CancellationToken cancellationToken)
        => GuardedAsync(async () =>
        {
            if (IsLeftOut(request, blocked))
            {
                await request.AbortAsync(RequestAbortErrorCode.BlockedByClient).ConfigureAwait(false);
            }
            else if (await _policy.AllowsAsync(request.Url, cancellationToken).ConfigureAwait(false))
            {
                await request.ContinueAsync().ConfigureAwait(false);
            }
            else
            {
                LogBlocked(_logger, new Uri(request.Url, UriKind.RelativeOrAbsolute).Host);
                await request.AbortAsync(RequestAbortErrorCode.BlockedByClient).ConfigureAwait(false);
            }
        });

    private static WaitUntilNavigation WaitFor(WaitCondition condition) => condition switch
    {
        WaitCondition.DomContentLoaded => WaitUntilNavigation.DOMContentLoaded,
        WaitCondition.NetworkIdle => WaitUntilNavigation.Networkidle0,
        _ => WaitUntilNavigation.Load,
    };

    private async Task<IBrowser> BrowserAsync(CancellationToken cancellationToken)
    {
        await _launch.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IBrowser? current = _browser;
            bool worn = _options.RecycleAfterPages > 0 && _rendered >= _options.RecycleAfterPages && Volatile.Read(ref _active) <= 1;
            if (current is { IsConnected: true, IsClosed: false } && !worn)
            {
                _rendered++;
                return current;
            }

            if (current is not null)
            {
                _browser = null;
                await CloseAsync(current).ConfigureAwait(false);
            }

            _browser = await LaunchAsync(cancellationToken).ConfigureAwait(false);
            _rendered = 1;
            return _browser;
        }
        finally
        {
            _launch.Release();
        }
    }

    private async Task<IBrowser> LaunchAsync(CancellationToken cancellationToken)
    {
        List<string> arguments = ["--disable-gpu", "--disable-extensions", "--disable-background-networking", "--disable-component-update", "--no-first-run", "--mute-audio"];
        if (_options.DisableSandbox)
        {
            arguments.Add("--no-sandbox");
        }

        if (_options.DisableDevShmUsage)
        {
            arguments.Add("--disable-dev-shm-usage");
        }

        if (_options.ProxyServer is { Length: > 0 } proxy)
        {
            arguments.Add($"--proxy-server={proxy}");
        }

        arguments.AddRange(_options.ExtraArguments);
        LaunchOptions launch = new()
        {
            Headless = true,
            ExecutablePath = _options.ExecutablePath,
            Args = [.. arguments],
            AcceptInsecureCerts = _options.IgnoreCertificateErrors,
            Timeout = (int)TimeSpan.FromSeconds(_options.OperationTimeoutSeconds).TotalMilliseconds,
        };

        for (int attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                IBrowser browser = await PuppeteerSharp.Puppeteer.LaunchAsync(launch).ConfigureAwait(false);
                LogLaunched(_logger);
                return browser;
            }
            catch (Exception exception) when (attempt < _options.LaunchAttempts && exception is not OperationCanceledException)
            {
                LogLaunchFailed(_logger, exception, attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task CloseAsync(IBrowser browser)
        => GuardedAsync(async () =>
        {
            await browser.CloseAsync().ConfigureAwait(false);
            await browser.DisposeAsync().ConfigureAwait(false);
        });

    /// <summary>Does a step that may find its page or browser already gone (the page closed, Chrome ended): that is logged and is not an error.</summary>
    internal async Task GuardedAsync(Func<Task> step)
    {
        try
        {
            await step().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is PuppeteerException or OperationCanceledException or InvalidOperationException or ObjectDisposedException)
        {
            LogGone(_logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Chrome started for the browser renderer")]
    private static partial void LogLaunched(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Chrome could not be started (attempt {Attempt})")]
    private static partial void LogLaunchFailed(ILogger logger, Exception exception, int attempt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A rendered page asked for a host that the policy does not allow: {Host}")]
    private static partial void LogBlocked(ILogger logger, string host);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The browser ended a request or a page that was already gone")]
    private static partial void LogGone(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The browser renderer stopped with {Pages} pages still being rendered")]
    private static partial void LogDrainTimeout(ILogger logger, int pages);
}
