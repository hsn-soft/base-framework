using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerBrowser : IPuppeteerBrowser
{
    private readonly IBaseLogger _logger;
    private readonly PuppeteerBrowserSettings _browserSettings;
    private readonly CancellationToken _applicationStoppingToken;

    // initialization
    [CanBeNull] private IBrowser _ptBrowser;
    [CanBeNull] private Task _initializationTask;
    private bool _disposed;

    // browser limit control
    private readonly SemaphoreSlim _browserSessionSemaphore;
    private readonly SemaphoreSlim _pageSemaphore;

    // Page pool
    private readonly ConcurrentQueue<(IPage Page, DateTime LastUsed)> _pages = new();
    private int _currentPageCount;
    private readonly TimeSpan _freePageWaitTimeout;

    // cleanup fields
    [CanBeNull] private readonly Task _cleanupTask;

    private readonly TimeSpan _cleanupInterval; // check idle pages in one minute
    private readonly TimeSpan _pageMaxIdleTime; // page max lifetime

    // public fields
    public int ActivePagesCount => Interlocked.CompareExchange(ref _currentPageCount, 0, 0);
    public int PooledPagesCount => _pages.Count;
    public int MaxPagesCount => _browserSettings.PageMaxCount;
    public string InitializationResult { get; private set; } = string.Empty;
    public bool HasProxyServer { get; private set; }
    public string[] Args { get; private set; } = [];

    public PuppeteerBrowser(
        IBaseLogger logger,
        IOptions<PuppeteerBrowserSettings> settings,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _browserSettings = settings?.Value ?? new PuppeteerBrowserSettings();

        // cancellation operation
        _applicationStoppingToken = appLifetime.ApplicationStopping;
        appLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Application stopping, disposing browser...");
            Dispose();
        });

        // initialize
        _browserSessionSemaphore = new SemaphoreSlim(1, 1);
        _pageSemaphore = new SemaphoreSlim(_browserSettings.PageMaxCount, _browserSettings.PageMaxCount);
        _freePageWaitTimeout = TimeSpan.FromMilliseconds(_browserSettings.WaitFreePageTimeoutMs);
        _initializationTask = InitializeAsync(_applicationStoppingToken);

        // Cleanup worker
        if (_browserSettings.CleanIdlePages)
        {
            _cleanupInterval = TimeSpan.FromMilliseconds(_browserSettings.CleanUpIntervalMs); // check idle pages in one minute
            _pageMaxIdleTime = TimeSpan.FromMilliseconds(_browserSettings.CleaningPageMaxIdleTimeMs); // page max lifetime
            _cleanupTask = Task.Run(() => CleanupIdlePagesLoopAsync(_applicationStoppingToken));
        }
    }

    public async Task<IBrowser> GetBrowserSafelyAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _applicationStoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning($"{nameof(PuppeteerBrowser)} | GetBrowserSafelyAsync requested but browser is disposing/stopping.");
            return null;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStoppingToken, cancellationToken);

        await _browserSessionSemaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        try
        {
            if (_initializationTask != null)
            {
                await _initializationTask.ConfigureAwait(false);
            }

            if (linkedCts.IsCancellationRequested)
            {
                _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Cannot return browser, application is stopping.");
                return null;
            }

            if (_ptBrowser is { IsConnected: true, IsClosed: false })
            {
                return _ptBrowser;
            }

            SafeDisposeBrowser();

            if (linkedCts.IsCancellationRequested)
            {
                _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Cancellation requested before re-initializing browser.");
                return null;
            }

            _initializationTask = InitializeAsync(linkedCts.Token);
            if (_initializationTask != null)
                await _initializationTask.ConfigureAwait(false);

            return _ptBrowser is { IsConnected: true, IsClosed: false } ? _ptBrowser : null;
        }
        finally
        {
            _browserSessionSemaphore.Release();
        }
    }

    public async Task<IPage> GetPoolPageAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _applicationStoppingToken.IsCancellationRequested)
            throw new OperationCanceledException($"{nameof(PuppeteerBrowser)} is disposed or application stopping.");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStoppingToken, cancellationToken);

        if (!await _pageSemaphore.WaitAsync(_freePageWaitTimeout, linkedCts.Token).ConfigureAwait(false))
            throw new TimeoutException($"Timeout waiting for a free Puppeteer page (max {_browserSettings.PageMaxCount})");

        var browser = await GetBrowserSafelyAsync(linkedCts.Token).ConfigureAwait(false);
        if (browser == null)
        {
            _pageSemaphore.Release();
            throw new OperationCanceledException("Browser is not available or shutting down");
        }

        // Has ready page on queue?
        while (_pages.TryDequeue(out var tuple))
        {
            if (!tuple.Page.IsClosed)
                return tuple.Page;

            SafeDisposePage(tuple.Page);
        }

        var page = await browser.NewPageAsync().ConfigureAwait(false);
        page.DefaultTimeout = _browserSettings.PageDefaultTimeoutMs;
        page.DefaultNavigationTimeout = _browserSettings.PageDefaultNavigationTimeoutMs;

        Interlocked.Increment(ref _currentPageCount);

        page.Close += (_, _) => { Interlocked.Decrement(ref _currentPageCount); };

        return page;
    }

    public void ReturnPoolPage(IPage page)
    {
        if (page == null)
            return;

        if (_disposed || _applicationStoppingToken.IsCancellationRequested || page.IsClosed)
        {
            SafeDisposePage(page);
            return;
        }

        _pages.Enqueue((page, DateTime.UtcNow));
        _pageSemaphore.Release();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Browser terminating...");

            if (_cleanupTask != null)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(5000);
                    Task.WhenAny(_cleanupTask, Task.Delay(Timeout.Infinite, timeoutCts.Token))
                        .GetAwaiter()
                        .GetResult();
                }
                catch
                {
                    // ignore
                }
            }

            while (_pages.TryDequeue(out var tuple))
            {
                SafeDisposePage(tuple.Page);
            }

            try
            {
                _browserSessionSemaphore.Wait(2000);
            }
            catch
            {
                // ignore
            }

            SafeDisposeBrowser();

            _browserSessionSemaphore.Dispose();
            _pageSemaphore.Dispose();

            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Browser terminated");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(PuppeteerBrowser)} | Error while disposing browser, {ex.Message}");
        }

        // Force memory clean and wait finalizers
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    #region Private Methods

    private async Task<LaunchOptions> CheckAndGetLaunchOptions()
    {
        List<string> defaultArgs =
        [
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
        List<string> checkedArgs = _browserSettings.Args is { Length: > 0 }
            ? _browserSettings.Args
                .Where(arg => !arg.StartsWith("--proxy-server", StringComparison.CurrentCultureIgnoreCase))
                .ToList()
            : defaultArgs;

        // Check proxy server
        bool proxyAdded = false;
        try
        {
            string proxyHost = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_HOST");
            string proxyPort = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_PORT");
            if (!string.IsNullOrWhiteSpace(proxyHost) && !string.IsNullOrWhiteSpace(proxyPort))
            {
                checkedArgs.Add($"--proxy-server=http://{proxyHost}:{proxyPort}");
                proxyAdded = true;
                _logger.LogDebug($"{nameof(PuppeteerBrowser)} | PROXY_SERVER_ADDED => http://{proxyHost}:{proxyPort}");
            }
        }
        catch (Exception)
        {
            _logger.LogWarning($"{nameof(PuppeteerBrowser)} | PROXY_SERVER_DEFINITION_FAILED");
        }

        if (!proxyAdded)
        {
            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | PROXY_SERVER_DEFINITION_SKIPPED");
        }

        Args = checkedArgs.ToArray();
        HasProxyServer = Args.Any(x => x.StartsWith("--proxy-server", StringComparison.CurrentCultureIgnoreCase));

        var launchOptions = new LaunchOptions { Headless = _browserSettings.Headless, LogProcess = _browserSettings.LogProcess, Args = Args };

        string inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        bool skipDownloadOperation = !string.IsNullOrWhiteSpace(inContainer) && inContainer == "true";

        if (!skipDownloadOperation)
        {
            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | RUNNING_IN_CONTAINER => false");

            var browserFetcher = new BrowserFetcher();
            var installedBrowsers = browserFetcher.GetInstalledBrowsers();

            var browserInfo = launchOptions.Headless
                ? installedBrowsers.FirstOrDefault(x => x.Browser == SupportedBrowser.ChromeHeadlessShell)
                : installedBrowsers.FirstOrDefault(x => x.Browser != SupportedBrowser.ChromeHeadlessShell);

            if (browserInfo == null)
            {
                _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Installed browser not found");
                _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Chromium download START");
                await browserFetcher.DownloadAsync().ConfigureAwait(false);
                installedBrowsers = browserFetcher.GetInstalledBrowsers();
                browserInfo = installedBrowsers.FirstOrDefault();

                if (browserInfo == null)
                {
                    _logger.LogError($"{nameof(PuppeteerBrowser)} | Chromium download FAILED");
                    throw new InvalidOperationException("Puppeteer | Installed browser not found");
                }

                _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Chromium download COMPLETED");
            }
            else
            {
                _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Chromium download SKIPPED => Browser is already installed");
            }

            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Browser [" + browserInfo.BuildId + "] founded");
            launchOptions.ExecutablePath = browserInfo.GetExecutablePath();
        }
        else
        {
            // override headless mode for container
            launchOptions.Headless = true;
            _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Chromium download SKIPPED => Container Mode is Active");
            launchOptions.ExecutablePath = "/usr/bin/chromium";
        }

        _logger.LogDebug($"{nameof(PuppeteerBrowser)} | EXECUTABLE_PATH => " + launchOptions.ExecutablePath);

        return launchOptions;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Browser is initializing ...");

        try
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Launch attempt {attempt}");

                try
                {
                    var launchOptions = await CheckAndGetLaunchOptions().ConfigureAwait(false);
                    _ptBrowser = await Puppeteer.LaunchAsync(launchOptions).ConfigureAwait(false);

                    if (_ptBrowser is { IsConnected: true, IsClosed: false })
                    {
                        // in Browser crash / disconnect, self-healing handler
                        _ptBrowser.Disconnected += (_, _) =>
                        {
                            _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Browser disconnected, will re-initialize on next request.");
                            _ptBrowser = null;
                            InitializationResult = "Browser disconnected";
                        };

                        InitializationResult = "Browser successfully initialized";
                        _logger.LogInformation($"{nameof(PuppeteerBrowser)} | {InitializationResult}");
                        return;
                    }

                    throw new Exception("Puppeteer browser unavailable");
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Launch attempt {attempt} failed, retrying... ({ex.Message})");
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }

            InitializationResult = "Browser initialize error: max retry exceeded";
        }
        catch (OperationCanceledException)
        {
            InitializationResult = "Browser initialize canceled";
        }
        catch (Exception e)
        {
            InitializationResult = "Browser initialize error: " + e.Message;
        }
        finally
        {
            if (!string.IsNullOrEmpty(InitializationResult) &&
                !InitializationResult.StartsWith("Browser successfully", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"{nameof(PuppeteerBrowser)} | {InitializationResult}");
            }

            if (_ptBrowser == null || !_ptBrowser.IsConnected || _ptBrowser.IsClosed)
            {
                _initializationTask = null;
            }
        }
    }

    private async Task CleanupIdlePagesLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                await Task.Delay(_cleanupInterval, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Page Cleanup | START");

                var now = DateTime.UtcNow;
                var tempList = new List<(IPage Page, DateTime LastUsed)>();

                while (_pages.TryDequeue(out var tuple))
                {
                    if (tuple.Page.IsClosed || now - tuple.LastUsed > _pageMaxIdleTime)
                    {
                        _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Page Cleanup | idle page is founded, closing page...");
                        SafeDisposePage(tuple.Page);
                        _logger.LogWarning($"{nameof(PuppeteerBrowser)} | Page Cleanup | idle page is closed");
                    }
                    else
                    {
                        tempList.Add(tuple);
                    }
                }

                foreach (var item in tempList)
                    _pages.Enqueue(item);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PuppeteerBrowser)} | Page Cleanup | Error, {ex.Message}");
            }
            finally
            {
                // After clean operation, call light garbage collector
                GC.Collect();

                _logger.LogDebug($"{nameof(PuppeteerBrowser)} | Page Cleanup | END");
            }
        }
    }

    private static void SafeDisposePage(IPage page)
    {
        if (page == null) return;

        try
        {
            if (!page.IsClosed)
            {
                page.CloseAsync().GetAwaiter().GetResult();
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            page.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    private void SafeDisposeBrowser()
    {
        if (_ptBrowser == null) return;

        try
        {
            if (!_ptBrowser.IsClosed)
            {
                _ptBrowser.CloseAsync().GetAwaiter().GetResult();
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            _ptBrowser.Dispose();
        }
        catch
        {
            // ignored
        }

        _ptBrowser = null;
    }

    #endregion
}