using HsnSoft.Base.PuppeTeer;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hhs.TextNormalizerService.HealthChecks;

public sealed class PuppeTeerHealthCheck(IPuppeteerBrowser puppeteerBrowser) : IHealthCheck
{
    private readonly IPuppeteerBrowser _puppeteerBrowser = puppeteerBrowser ?? throw new ArgumentNullException(nameof(puppeteerBrowser));

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.FromResult(_puppeteerBrowser.IsStopping
        ? HealthCheckResult.Unhealthy(description: "Puppeteer browser is stopping.")
        : HealthCheckResult.Healthy(description: "Puppeteer browser is ready."));
}