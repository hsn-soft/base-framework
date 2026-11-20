using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Browser.Puppeteer;

/// <summary>
/// Ready when the Chrome of <c>Browser:ExecutablePath</c> is there, or when one would be downloaded (<c>DownloadWhenMissing</c> outside a container); it is not started for a probe: that would be a browser for every probe.
/// </summary>
internal sealed class PuppeteerHealthCheck(IOptions<PuppeteerOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        PuppeteerOptions value = options.Value;
        if (!string.IsNullOrWhiteSpace(value.ExecutablePath) && File.Exists(value.ExecutablePath))
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        using BrowserExecutable executable = new(value, NullLogger.Instance);
        return Task.FromResult(executable.WouldDownload
            ? HealthCheckResult.Healthy("The Chrome is not there yet: it is downloaded when the first page is rendered.")
            : HealthCheckResult.Unhealthy("The Chrome of Browser:ExecutablePath is not there."));
    }
}
