using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

// containers are heavy; start them one test class after the other
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HsnSoft.Base.IntegrationTests;

internal static class Support
{
    public static IConfiguration Configuration(params (string Key, string? Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))).Build();

    public static async Task<IHost> StartHostAsync(Action<IServiceCollection> configure)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        configure(builder.Services);

        IHost host = builder.Build();
        await host.StartAsync();
        return host;
    }

    /// <summary>Retries <paramref name="attempt"/> until it returns true; fails the test after <paramref name="timeout"/>.</summary>
    public static async Task EventuallyAsync(Func<Task<bool>> attempt, string what, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (!await attempt())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"Timed out waiting for: {what}");
            }

            await Task.Delay(250);
        }
    }

    public static async Task<HealthReport> ReadyReportAsync(IServiceProvider services)
        => await services.GetRequiredService<HealthCheckService>().CheckHealthAsync(registration => registration.Tags.Contains("ready"));
}
