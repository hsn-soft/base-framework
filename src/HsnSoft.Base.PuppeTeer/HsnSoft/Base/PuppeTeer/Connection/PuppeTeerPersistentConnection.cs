using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer.Connection;

public sealed class PuppeTeerPersistentConnection : IPuppeTeerPersistentConnection
{
    private readonly IBaseLogger _logger;
    private readonly PuppeTeerConnectionSettings _launchSettings;
    private readonly SemaphoreSlim _browserSessionSemaphore;
    private IBrowser PtBrowser { get; set; }
    private bool _disposed;

    public string InitResult { get; set; }

    public PuppeTeerPersistentConnection(IBaseLogger logger, IOptions<PuppeTeerConnectionSettings> settings)
    {
        _logger = logger;
        _browserSessionSemaphore = new SemaphoreSlim(1, 1);
        _launchSettings = settings?.Value ?? new PuppeTeerConnectionSettings();
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        _logger.LogDebug("Puppeteer | Browser is initializing ...");

        try
        {
            var launchOptions = new LaunchOptions
            {
                Headless = _launchSettings.Headless,
                LogProcess = _launchSettings.LogProcess,
                Args = _launchSettings.Args is { Length: > 0 }
                    ? _launchSettings.Args
                    : new[]
                    {
                        "--no-sandbox",
                        "--disable-gpu",
                        "--disable-dev-shm-usage",
                        "--disable-setuid-sandbox",
                        "--disable-web-security",
                        "--disable-features=IsolateOrigins,site-per-process"
                    }
            };

            var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            var skipDownloadOperation = !string.IsNullOrWhiteSpace(inContainer) && inContainer == "true";
            if (!skipDownloadOperation)
            {
                _logger.LogDebug("Puppeteer | RUNNING_IN_CONTAINER => false");

                var browserFetcher = new BrowserFetcher();
                var installedBrowsers = browserFetcher.GetInstalledBrowsers();

                var browserInfo = _launchSettings.Headless
                    ? installedBrowsers.FirstOrDefault(x => x.Browser == SupportedBrowser.ChromeHeadlessShell)
                    : installedBrowsers.FirstOrDefault(x => x.Browser != SupportedBrowser.ChromeHeadlessShell);

                if (browserInfo == null)
                {
                    _logger.LogWarning("Puppeteer | Installed browser not found");
                    _logger.LogDebug("Puppeteer | Chromium download START");
                    await browserFetcher.DownloadAsync();

                    installedBrowsers = browserFetcher.GetInstalledBrowsers();
                    browserInfo = installedBrowsers.First();
                    if (browserInfo == null)
                    {
                        _logger.LogError("Puppeteer | Chromium download FAILED");
                        throw new InvalidOperationException("Puppeteer | Installed browser not found");
                    }

                    _logger.LogDebug("Puppeteer | Chromium download COMPLETED");
                }
                else
                {
                    _logger.LogDebug("Puppeteer | Chromium download SKIPPED => Browser is already installed");
                }

                _logger.LogDebug("Puppeteer | Browser [" + browserInfo.BuildId + "] founded");
                launchOptions.ExecutablePath = browserInfo.GetExecutablePath();
            }
            else
            {
                // override headless mode for container
                launchOptions.Headless = true;
                _logger.LogDebug("Puppeteer | Chromium download SKIPPED => Container Mode is Active");

                launchOptions.ExecutablePath = "/usr/bin/chromium";
            }

            _logger.LogDebug("Puppeteer | EXECUTABLE_PATH => " + launchOptions.ExecutablePath);

            PtBrowser = await Puppeteer.LaunchAsync(launchOptions);

            if (PtBrowser is { IsConnected: true, IsClosed: false })
            {
                InitResult = "Puppeteer | Browser is successfully initialized";
                _logger.LogInformation(InitResult);
            }
            else
            {
                throw new Exception("Browser is not null or connection closed");
            }
        }
        catch (Exception e)
        {
            InitResult = "Puppeteer | Browser initialize fail: " + e.Message;
            _logger.LogError(InitResult);
        }
    }

    public IBrowser GetBrowser()
    {
        _browserSessionSemaphore.Wait();

        try
        {
            // Safely get PT Browser
            if (PtBrowser is { IsConnected: true, IsClosed: false })
            {
                return PtBrowser;
            }

            // Safely shut-down PT Browser
            try
            {
                PtBrowser.Disconnect();
                PtBrowser?.Dispose();
                PtBrowser = null;
            }
            catch (Exception)
            {
                // ignored
            }

            // Re-Initialize PT Browser
            InitializeAsync().GetAwaiter().GetResult();

            return PtBrowser is { IsConnected: true, IsClosed: false } ? PtBrowser : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            _browserSessionSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            PtBrowser?.Dispose();
            _browserSessionSemaphore?.Dispose();
            _logger.LogDebug("Puppeteer | Terminated");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}