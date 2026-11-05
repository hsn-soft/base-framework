using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Browser.Puppeteer;

/// <summary>Ready when the Chrome of <c>Browser:ExecutablePath</c> is there (it is not started for a probe: that would be a browser for every probe).</summary>
internal sealed class PuppeteerHealthCheck(IOptions<PuppeteerOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(options.Value.ExecutablePath) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("The Chrome of Browser:ExecutablePath is not there."));
}
