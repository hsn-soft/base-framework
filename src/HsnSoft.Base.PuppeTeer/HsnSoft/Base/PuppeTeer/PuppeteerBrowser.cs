using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Helpers;

namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerBrowser : IPuppeteerBrowser
{
    private readonly IBaseLogger _logger;
    private readonly PuppeteerBrowserSettings _settings;
    private readonly CancellationTokenSource _serviceCts;

    // browser init / replace / dispose gate
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    // max concurrent page limit
    private readonly SemaphoreSlim _pageLimiter;

    // browser state
    private IBrowser? _browser;
    private Task? _initializationTask;

    // shutdown / dispose state
    private int _shutdownRequested;
    private int _disposeStarted;
    private bool _disposed;

    // active pages
    private int _activePages;

    // graceful drain
    private TaskCompletionSource<bool> _drainedTcs = NewDrainedTcs();

    public int ActivePagesCount => Volatile.Read(ref _activePages);
    public int MaxPagesCount => _settings.PageMaxCount;
    public string InitializationResult { get; private set; } = string.Empty;
    public bool HasProxyServer { get; private set; }
    public string[] Args { get; private set; } = [];
    public bool IsStopping => Volatile.Read(ref _shutdownRequested) == 1 || _serviceCts.IsCancellationRequested || _disposed;

    public PuppeteerBrowser(
        IBaseLogger logger,
        IOptions<PuppeteerBrowserSettings>? settings,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? new PuppeteerBrowserSettings();

        _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);

        _pageLimiter = new SemaphoreSlim(_settings.PageMaxCount, _settings.PageMaxCount);

        TaskHelper.DefaultTimeout = 30 * 60 * 1000;

        // eager init
        _initializationTask = EnsureBrowserInitializedCoreAsync(_serviceCts.Token);

        appLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("{Service} | ApplicationStopping received", nameof(PuppeteerBrowser));
            _ = Task.Run(async () =>
            {
                try
                {
                    await RequestShutdownAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            });
        });
    }

    public async Task<IBrowser?> GetBrowserSafelyAsync(CancellationToken cancellationToken = default)
    {
        if (IsStopping)
        {
            _logger.LogWarning("{Service} | GetBrowserSafelyAsync requested but service is stopping/disposed", nameof(PuppeteerBrowser));
            return null;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _serviceCts.Token);

        Task? initTaskToAwait;

        await _lifecycleGate.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        try
        {
            if (IsStopping)
                return null;

            if (_browser is { IsConnected: true, IsClosed: false })
                return _browser;

            if (_initializationTask == null || _initializationTask.IsCompleted)
                _initializationTask = EnsureBrowserInitializedCoreAsync(_serviceCts.Token);

            initTaskToAwait = _initializationTask;
        }
        finally
        {
            _lifecycleGate.Release();
        }

        await initTaskToAwait.ConfigureAwait(false);

        await _lifecycleGate.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        try
        {
            if (IsStopping)
                return null;

            return _browser is { IsConnected: true, IsClosed: false } ? _browser : null;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<PuppeteerPageLease> AcquirePageAsync(
        Func<IPage, CancellationToken, Task>? configurePage = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsStopping)
            throw new OperationCanceledException($"{nameof(PuppeteerBrowser)} shutdown requested. No new page lease accepted.");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _serviceCts.Token);

        var waitTimeout = TimeSpan.FromMilliseconds(_settings.WaitFreePageTimeoutMs);
        var limiterAcquired = false;
        IPage? page = null;

        try
        {
            if (!await _pageLimiter.WaitAsync(waitTimeout, linkedCts.Token).ConfigureAwait(false))
                throw new TimeoutException($"Timeout waiting for a free Puppeteer page slot. Max concurrent pages: {_settings.PageMaxCount}");

            limiterAcquired = true;

            if (IsStopping)
                throw new OperationCanceledException($"{nameof(PuppeteerBrowser)} shutdown requested. No new page lease accepted.");

            var browser = await GetBrowserSafelyAsync(linkedCts.Token).ConfigureAwait(false);
            if (browser == null)
                throw new InvalidOperationException("Puppeteer browser is not available.");

            page = await browser.NewPageAsync().ConfigureAwait(false);

            page.DefaultTimeout = _settings.DefaultPageTimeoutMs;
            page.DefaultNavigationTimeout = _settings.DefaultNavigationTimeoutMs;

            Interlocked.Increment(ref _activePages);

            if (configurePage != null)
                await configurePage(page, linkedCts.Token).ConfigureAwait(false);

            _logger.LogDebug("{Service} | Page leased | ActivePages={ActivePages}", nameof(PuppeteerBrowser), ActivePagesCount);

            return new PuppeteerPageLease(page, ReleasePageInternalAsync);
        }
        catch
        {
            if (page != null)
                await SafeClosePageAsync(page).ConfigureAwait(false);

            if (limiterAcquired)
                _pageLimiter.Release();

            throw;
        }
    }

    public Task RequestShutdownAsync()
    {
        RequestShutdown();
        return Task.CompletedTask;
    }

    public async Task<bool> RequestShutdownAndDrainAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        RequestShutdown();

        var active = ActivePagesCount;
        _logger.LogWarning(
            "{Service} | Drain requested | ActivePages={ActivePages} | Timeout={TimeoutSeconds}s",
            nameof(PuppeteerBrowser),
            active,
            timeout.TotalSeconds);

        if (active <= 0)
        {
            _drainedTcs.TrySetResult(true);
            return true;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        try
        {
            await _drainedTcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);

            _logger.LogWarning("{Service} | Drain completed", nameof(PuppeteerBrowser));
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "{Service} | Drain timeout | ActivePages={ActivePages}",
                nameof(PuppeteerBrowser),
                ActivePagesCount);

            return false;
        }
    }

    public void Dispose() { DisposeAsync().AsTask().GetAwaiter().GetResult(); }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) == 1)
            return;

        RequestShutdown();

        try
        {
            _logger.LogInformation("{Service} | DisposeAsync starting", nameof(PuppeteerBrowser));

            IBrowser? browserToClose;

            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed)
                    return;

                _disposed = true;
                browserToClose = _browser;
                _browser = null;
                _initializationTask = null;
            }
            finally
            {
                _lifecycleGate.Release();
            }

            if (browserToClose != null)
                await SafeDisposeBrowserAsync(browserToClose).ConfigureAwait(false);

            _logger.LogInformation("{Service} | DisposeAsync completed", nameof(PuppeteerBrowser));
        }
        catch (Exception ex)
        {
            _logger.LogError("{Service} | DisposeAsync failed: {Error}", nameof(PuppeteerBrowser), ex.Message);
        }
        finally
        {
            try
            {
                _serviceCts.Dispose();
            }
            catch
            {
                /* ignore */
            }

            try
            {
                _lifecycleGate.Dispose();
            }
            catch
            {
                /* ignore */
            }

            try
            {
                _pageLimiter.Dispose();
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PuppeteerBrowser));
    }

    private static TaskCompletionSource<bool> NewDrainedTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void RequestShutdown()
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
            return;

        _logger.LogInformation("{Service} | Shutdown requested", nameof(PuppeteerBrowser));

        try
        {
            _serviceCts.Cancel();
        }
        catch
        {
            // ignore
        }

        if (ActivePagesCount <= 0)
            _drainedTcs.TrySetResult(true);
    }

    private async Task ReleasePageInternalAsync(IPage page)
    {
        try
        {
            await SafeClosePageAsync(page).ConfigureAwait(false);
        }
        finally
        {
            var left = Interlocked.Decrement(ref _activePages);

            if (left < 0)
            {
                Interlocked.Exchange(ref _activePages, 0);
                left = 0;
                _logger.LogWarning("{Service} | Active page counter went below zero", nameof(PuppeteerBrowser));
            }

            try
            {
                _pageLimiter.Release();
            }
            catch
            {
                // ignore
            }

            _logger.LogDebug("{Service} | Page released | ActivePages={ActivePages}", nameof(PuppeteerBrowser), left);

            if (IsStopping && left <= 0)
                _drainedTcs.TrySetResult(true);
        }
    }

    private async Task EnsureBrowserInitializedCoreAsync(CancellationToken cancellationToken)
    {
        if (IsStopping)
            return;

        _logger.LogDebug("{Service} | Browser initializing...", nameof(PuppeteerBrowser));

        IBrowser? launchedBrowser = null;

        try
        {
            var maxRetries = Math.Max(1, _settings.BrowserLaunchRetryCount);

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogDebug("{Service} | Launch attempt {Attempt}", nameof(PuppeteerBrowser), attempt);

                    var launchOptions = await CheckAndGetLaunchOptionsAsync().ConfigureAwait(false);
                    launchedBrowser = await Puppeteer.LaunchAsync(launchOptions).ConfigureAwait(false);

                    if (launchedBrowser is { IsConnected: true, IsClosed: false })
                        break;

                    await SafeDisposeBrowserAsync(launchedBrowser).ConfigureAwait(false);
                    launchedBrowser = null;

                    throw new InvalidOperationException("Puppeteer browser unavailable after launch.");
                }
                catch (Exception ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "{Service} | Launch attempt {Attempt} failed: {Error}. Retrying...",
                        nameof(PuppeteerBrowser),
                        attempt,
                        ex.Message);

                    var delaySec = Math.Max(1, _settings.BrowserLaunchRetryDelaySeconds) * attempt;
                    await Task.Delay(TimeSpan.FromSeconds(delaySec), cancellationToken).ConfigureAwait(false);
                }
            }

            if (launchedBrowser == null)
            {
                InitializationResult = "Browser initialize error: max retry exceeded";
                _logger.LogError("{Service} | {Result}", nameof(PuppeteerBrowser), InitializationResult);
                return;
            }

            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsStopping)
                {
                    _logger.LogWarning("{Service} | Browser launched during shutdown; closing without publish", nameof(PuppeteerBrowser));
                }
                else
                {
                    _browser = launchedBrowser;

                    _browser.Disconnected += (_, _) =>
                    {
                        _logger.LogWarning("{Service} | Browser disconnected", nameof(PuppeteerBrowser));

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _lifecycleGate.WaitAsync().ConfigureAwait(false);
                                try
                                {
                                    if (ReferenceEquals(_browser, launchedBrowser))
                                    {
                                        _browser = null;
                                        _initializationTask = null;
                                        InitializationResult = "Browser disconnected";
                                    }
                                }
                                finally
                                {
                                    _lifecycleGate.Release();
                                }
                            }
                            catch
                            {
                                // ignore
                            }
                        });
                    };

                    InitializationResult = "Browser successfully initialized";
                    _logger.LogInformation("{Service} | {Result}", nameof(PuppeteerBrowser), InitializationResult);
                    return;
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }

            await SafeDisposeBrowserAsync(launchedBrowser).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            InitializationResult = "Browser initialize canceled";
            _logger.LogWarning("{Service} | {Result}", nameof(PuppeteerBrowser), InitializationResult);

            if (launchedBrowser != null)
                await SafeDisposeBrowserAsync(launchedBrowser).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            InitializationResult = "Browser initialize error: " + ex.Message;
            _logger.LogError("{Service} | {Result}", nameof(PuppeteerBrowser), InitializationResult);

            if (launchedBrowser != null)
                await SafeDisposeBrowserAsync(launchedBrowser).ConfigureAwait(false);
        }
        finally
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_browser == null || !_browser.IsConnected || _browser.IsClosed)
                    _initializationTask = null;
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }
    }

    private async Task<LaunchOptions> CheckAndGetLaunchOptionsAsync()
    {
        List<string> defaultArgs =
        [
            "--headless=new",
            "--no-sandbox",
            "--disable-gpu",
            "--disable-dev-shm-usage",
            "--disable-setuid-sandbox",
            "--disable-extensions",
            "--disable-blink-features=AutomationControlled",
            "--ignore-certificate-errors",
            "--allow-insecure-localhost",
            "--disable-infobars",
            "--window-size=1920,1080",
            "--start-maximized",
            "--no-default-browser-check",
            "--disable-background-networking",
            "--disable-webgl"
        ];

        // Check external arguments
        var checkedArgs = _settings.Args is { Length: > 0 }
            ? _settings.Args
                .Where(arg => !arg.StartsWith("--proxy-server", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : defaultArgs;

        // Check proxy server
        var proxyAdded = false;
        try
        {
            var proxyHost = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_HOST");
            var proxyPort = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_PORT");
            if (!string.IsNullOrWhiteSpace(proxyHost) && !string.IsNullOrWhiteSpace(proxyPort))
            {
                checkedArgs.Add($"--proxy-server=https://{proxyHost}:{proxyPort}");
                proxyAdded = true;

                _logger.LogDebug("{Service} | PROXY_SERVER_ADDED => https://{ProxyHost}:{ProxyPort}", nameof(PuppeteerBrowser), proxyHost, proxyPort);
            }
        }
        catch
        {
            _logger.LogWarning("{Service} | PROXY_SERVER_DEFINITION_FAILED", nameof(PuppeteerBrowser));
        }

        if (!proxyAdded)
        {
            _logger.LogDebug("{Service} | PROXY_SERVER_DEFINITION_SKIPPED", nameof(PuppeteerBrowser));
        }

        Args = checkedArgs.ToArray();
        HasProxyServer = Args.Any(x => x.StartsWith("--proxy-server", StringComparison.OrdinalIgnoreCase));

        var launchOptions = new LaunchOptions { Headless = _settings.Headless, Args = Args };

        var downloadEnvironment = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        var skipDownloadOperation = !string.IsNullOrWhiteSpace(downloadEnvironment) && downloadEnvironment == "true";

        if (!skipDownloadOperation)
        {
            _logger.LogDebug("{Service} | DOTNET_RUNNING_IN_CONTAINER => false", nameof(PuppeteerBrowser));

            var browserFetcher = new BrowserFetcher();
            var installedBrowsers = browserFetcher.GetInstalledBrowsers();

            var browserInfo = launchOptions.Headless
                ? installedBrowsers.FirstOrDefault(x => x.Browser == SupportedBrowser.ChromeHeadlessShell)
                : installedBrowsers.FirstOrDefault(x => x.Browser != SupportedBrowser.ChromeHeadlessShell);

            if (browserInfo == null)
            {
                _logger.LogWarning("{Service} | Installed browser not found", nameof(PuppeteerBrowser));
                _logger.LogWarning("{Service} | Chromium download START", nameof(PuppeteerBrowser));
                await browserFetcher.DownloadAsync().ConfigureAwait(false);
                installedBrowsers = browserFetcher.GetInstalledBrowsers();
                browserInfo = installedBrowsers.FirstOrDefault();

                if (browserInfo == null)
                {
                    _logger.LogError("{Service} | Chromium download FAILED", nameof(PuppeteerBrowser));
                    throw new InvalidOperationException("Puppeteer | Installed browser not found");
                }

                _logger.LogWarning("{Service} | Chromium download COMPLETED", nameof(PuppeteerBrowser));
            }
            else
            {
                _logger.LogDebug("{Service} | Chromium download SKIPPED => Browser is already installed", nameof(PuppeteerBrowser));
            }

            _logger.LogDebug("{Service} | Browser [" + browserInfo.BuildId + "] founded", nameof(PuppeteerBrowser));
            launchOptions.ExecutablePath = browserInfo.GetExecutablePath();
        }
        else
        {
            _logger.LogDebug("{Service} | DOTNET_RUNNING_IN_CONTAINER => true", nameof(PuppeteerBrowser));

            // override headless mode for container
            launchOptions.Headless = true;
            launchOptions.ExecutablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH") ?? string.Empty;
        }

        _logger.LogDebug("{Service} | HEADLESS => " + (launchOptions.Headless ? "true" : "false"), nameof(PuppeteerBrowser));

        if (string.IsNullOrWhiteSpace(launchOptions.ExecutablePath))
        {
            _logger.LogWarning("{Service} | EXECUTABLE_PATH => UNKNOWN", nameof(PuppeteerBrowser));
        }
        else
        {
            _logger.LogDebug("{Service} | EXECUTABLE_PATH => " + launchOptions.ExecutablePath, nameof(PuppeteerBrowser));
        }

        return launchOptions;
    }

    private static async Task SafeClosePageAsync(IPage? page)
    {
        if (page == null)
            return;

        try
        {
            if (!page.IsClosed)
                await page.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        try
        {
            await page.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    }

    private static async Task SafeDisposeBrowserAsync(IBrowser? browser)
    {
        if (browser == null)
            return;

        try
        {
            if (!browser.IsClosed)
                await browser.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        try
        {
            browser.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}