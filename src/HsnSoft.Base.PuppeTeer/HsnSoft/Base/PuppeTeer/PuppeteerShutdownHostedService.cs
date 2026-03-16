using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerShutdownHostedService : IHostedService
{
    private readonly IPuppeteerBrowser _puppeteerBrowser;
    private readonly PuppeteerBrowserSettings _settings;

    public PuppeteerShutdownHostedService(
        IPuppeteerBrowser puppeteerBrowser,
        IOptions<PuppeteerBrowserSettings> settings)
    {
        _puppeteerBrowser = puppeteerBrowser ?? throw new ArgumentNullException(nameof(puppeteerBrowser));
        _settings = settings?.Value ?? new PuppeteerBrowserSettings();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var drainTimeout = TimeSpan.FromSeconds(_settings.ShutdownDrainTimeoutSeconds);

        try
        {
            await _puppeteerBrowser.RequestShutdownAsync().ConfigureAwait(false);
            await _puppeteerBrowser.RequestShutdownAndDrainAsync(drainTimeout, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _puppeteerBrowser.DisposeAsync().ConfigureAwait(false);
        }
    }
}