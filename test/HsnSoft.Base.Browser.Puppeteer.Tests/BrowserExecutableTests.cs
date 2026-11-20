using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Browser.Puppeteer.Tests;

public class BrowserExecutableTests
{
    private static Func<string, string?> Env(params (string Key, string Value)[] values)
    {
        Dictionary<string, string> map = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return key => map.GetValueOrDefault(key);
    }

    private static BrowserExecutable Make(PuppeteerOptions options, Func<string, string?> env, Func<string, bool> exists, List<string>? downloads = null)
        => new(options, NullLogger.Instance, env, exists, (folder, _) =>
        {
            downloads?.Add(folder);
            return Task.FromResult("/downloaded/chrome");
        });

    [Theory]
    [InlineData("DOTNET_RUNNING_IN_CONTAINER", "true", true)]
    [InlineData("DOTNET_RUNNING_IN_CONTAINER", "TRUE", true)]
    [InlineData("IS_IN_DOCKER_CONTAINER", "1", true)]
    [InlineData("KUBERNETES_SERVICE_HOST", "10.0.0.1", true)]
    [InlineData("IS_IN_DOCKER_CONTAINER", "false", false)]
    [InlineData("DOTNET_RUNNING_IN_CONTAINER", "", false)]
    public void AContainerIsRecognisedByItsMarks(string key, string value, bool expected)
        => Assert.Equal(expected, Make(new PuppeteerOptions(), Env((key, value)), _ => false).InContainer);

    [Fact]
    public void TheMarkerFileOfDocker_IsAContainer()
        => Assert.True(Make(new PuppeteerOptions(), Env(), path => path == "/.dockerenv").InContainer);

    [Fact]
    public void NoMark_IsAMachine()
        => Assert.False(Make(new PuppeteerOptions(), Env(), _ => false).InContainer);

    [Fact]
    public async Task TheChromeThatIsThere_IsUsed_AndNothingIsDownloaded()
    {
        List<string> downloads = [];
        using BrowserExecutable executable = Make(new PuppeteerOptions { ExecutablePath = "/usr/bin/chromium", DownloadWhenMissing = true }, Env(), path => path == "/usr/bin/chromium", downloads);

        Assert.Equal("/usr/bin/chromium", await executable.ResolveAsync(CancellationToken.None));
        Assert.Empty(downloads);
    }

    [Fact]
    public async Task AChromeThatIsMissing_IsDownloadedOnceOnAMachine_WhenTheFlagIsOn()
    {
        List<string> downloads = [];
        using BrowserExecutable executable = Make(new PuppeteerOptions { ExecutablePath = "/nowhere", DownloadWhenMissing = true, DownloadPath = "/cache" }, Env(), _ => false, downloads);

        Assert.Equal("/downloaded/chrome", await executable.ResolveAsync(CancellationToken.None));
        Assert.Equal("/downloaded/chrome", await executable.ResolveAsync(CancellationToken.None));
        Assert.Equal(["/cache"], downloads);
    }

    [Fact]
    public async Task TwoCallsAtOnce_DownloadOnce_AndTheSecondGetsTheSamePath()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int downloads = 0;
        using BrowserExecutable executable = new(new PuppeteerOptions { DownloadWhenMissing = true }, NullLogger.Instance, Env(), _ => false, async (_, _) =>
        {
            Interlocked.Increment(ref downloads);
            await release.Task;
            return "/downloaded/chrome";
        });

        Task<string> first = executable.ResolveAsync(CancellationToken.None);
        Task<string> second = executable.ResolveAsync(CancellationToken.None);
        release.SetResult();

        Assert.Equal(["/downloaded/chrome", "/downloaded/chrome"], await Task.WhenAll(first, second));
        Assert.Equal(1, downloads);
    }

    [Fact]
    public async Task NoPathAtAll_IsDownloaded_IntoTheDefaultFolder()
    {
        List<string> downloads = [];
        using BrowserExecutable executable = Make(new PuppeteerOptions { DownloadWhenMissing = true }, Env(), _ => false, downloads);

        await executable.ResolveAsync(CancellationToken.None);

        Assert.EndsWith(Path.Combine("HsnSoft", "browser"), Assert.Single(downloads), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InAContainer_NothingIsDownloaded_WhateverTheFlagSays()
    {
        List<string> downloads = [];
        using BrowserExecutable executable = Make(new PuppeteerOptions { ExecutablePath = "/nowhere", DownloadWhenMissing = true }, Env(("DOTNET_RUNNING_IN_CONTAINER", "true")), _ => false, downloads);

        Assert.False(executable.WouldDownload);
        Assert.Equal("/nowhere", await executable.ResolveAsync(CancellationToken.None));
        Assert.Empty(downloads);
    }

    [Fact]
    public async Task InAContainerWithNoPath_IsAClearError()
    {
        using BrowserExecutable executable = Make(new PuppeteerOptions { DownloadWhenMissing = true }, Env(("KUBERNETES_SERVICE_HOST", "10.0.0.1")), _ => false);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => executable.ResolveAsync(CancellationToken.None));

        Assert.Contains("never downloads", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithTheFlagOff_AMissingChromeIsNotDownloaded()
    {
        List<string> downloads = [];
        using BrowserExecutable executable = Make(new PuppeteerOptions { ExecutablePath = "/nowhere" }, Env(), _ => false, downloads);

        Assert.Equal("/nowhere", await executable.ResolveAsync(CancellationToken.None));
        Assert.Empty(downloads);
    }

    [Fact]
    public async Task WithTheFlagOffAndNoPath_IsAnError()
    {
        PuppeteerOptions options = new();
        using BrowserExecutable executable = Make(options, Env(), _ => false);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => executable.ResolveAsync(CancellationToken.None));

        Assert.Contains("Browser:ExecutablePath", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThePathIsRequired_UnlessTheDownloadIsOn()
    {
        Assert.Throws<InvalidOperationException>(() => new PuppeteerBrowserRenderer(Options.Create(new PuppeteerOptions()), NullLogger<PuppeteerBrowserRenderer>.Instance));

        await using PuppeteerBrowserRenderer renderer = new(Options.Create(new PuppeteerOptions { DownloadWhenMissing = true }), NullLogger<PuppeteerBrowserRenderer>.Instance);

        Assert.NotNull(renderer);
    }

    [Fact]
    public async Task TheRenderer_AsksTheExecutableOnItsFirstLaunch()
    {
        List<string> downloads = [];
        PuppeteerOptions options = new() { DownloadWhenMissing = true, LaunchAttempts = 1 };
        BrowserExecutable executable = new(options, NullLogger.Instance, Env(), _ => false, (folder, _) =>
        {
            downloads.Add(folder);
            return Task.FromResult("/nowhere/downloaded-chrome");
        });
        await using PuppeteerBrowserRenderer renderer = new(Options.Create(options), NullLogger<PuppeteerBrowserRenderer>.Instance, executable);

        await Assert.ThrowsAnyAsync<BrowserException>(() => renderer.GetPageAsync(BrowserRequest.FromHtml("<p>x</p>")));

        Assert.Single(downloads);
    }

    [Fact]
    public async Task TheHealthCheck_IsReadyWhenTheChromeIsThere_OrWouldBeDownloaded()
    {
        string here = Path.GetTempFileName();
        try
        {
            Assert.Equal(HealthStatus.Healthy, (await Check(new PuppeteerOptions { ExecutablePath = here })).Status);
            Assert.Equal(HealthStatus.Unhealthy, (await Check(new PuppeteerOptions { ExecutablePath = "/nowhere/chrome", DownloadWhenMissing = false })).Status);
        }
        finally
        {
            File.Delete(here);
        }

        static async Task<HealthCheckResult> Check(PuppeteerOptions options)
            => await new PuppeteerHealthCheck(Options.Create(options)).CheckHealthAsync(new HealthCheckContext());
    }
}
