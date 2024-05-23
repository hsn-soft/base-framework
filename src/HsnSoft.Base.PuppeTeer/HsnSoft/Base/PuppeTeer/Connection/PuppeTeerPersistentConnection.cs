using System.IO;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer.Connection;

public sealed class PuppeTeerPersistentConnection : IPuppeTeerPersistentConnection
{
    private readonly IBaseLogger _logger;
    private readonly IBrowser _browser;

    private bool _disposed;

    public PuppeTeerPersistentConnection(IBaseLogger logger)
    {
        _logger = logger;

        _browser = LaunchBrowserAsync().GetAwaiter().GetResult();
        _logger.LogInformation("Puppeteer | Initialized");
    }

    private async Task<IBrowser> LaunchBrowserAsync()
    {
        _logger.LogDebug("Puppeteer | Browser is initializing ...");

        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            //ExecutablePath = _puppeTeerExecutablePath,
            Headless = false,
            LogProcess = true,
            Args = new[]
            {
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-sandbox"
            }
        });
    }

    public IBrowser GetBrowser() => _browser;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _browser?.Dispose();
            _logger.LogDebug("Puppeteer | Terminated");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}