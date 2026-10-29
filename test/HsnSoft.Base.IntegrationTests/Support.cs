using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

// containers are heavy; start them one test class after the other
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HsnSoft.Base.IntegrationTests;

internal static class Support
{
    private static readonly System.Text.RegularExpressions.Regex s_distress = new(
        @"out of memory|oomkilled|\bfatal\b|\bpanic\b|too many open files|slow query|no space left|""s"":""[EF]""",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5));

    /// <summary>
    /// Removes a container, after saying how it ended up when something is wrong with it: it is not running any more (killed, out of memory) or its log has lines of distress (a fatal error, an out-of-memory, a slow query). A test that fails with a timeout
    /// because its database stood still leaves nothing else to look at once the container is gone. A healthy container says nothing.
    /// </summary>
    public static async Task DisposeAsync(IAsyncDisposable container, TextWriter? output = null)
    {
        if (container is IContainer running)
        {
            await ReportAsync(running, output ?? Console.Error).ConfigureAwait(false);
        }

        await container.DisposeAsync().ConfigureAwait(false);
    }

    internal static async Task ReportAsync(IContainer container, TextWriter output)
    {
        try
        {
            List<string> findings = [];
            if (container.State is not TestcontainersStates.Running)
            {
                findings.Add($"state {container.State}, exit code {await container.GetExitCodeAsync().ConfigureAwait(false)}");
            }

            (string stdout, string stderr) = await container.GetLogsAsync().ConfigureAwait(false);
            findings.AddRange(
                (stdout + "\n" + stderr)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => s_distress.IsMatch(line))
                    .Select(line => line.Length > 300 ? line[..300] : line)
                    .Take(15));

            if (findings.Count > 0)
            {
                await output.WriteLineAsync($"[container-diagnostics] {container.Image.FullName} ({container.Name}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", findings)}").ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // the report is for whoever reads a failed run: it never fails a test of its own
            await output.WriteLineAsync($"[container-diagnostics] could not read {container.Name}: {exception.Message}").ConfigureAwait(false);
        }
    }

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
