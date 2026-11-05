using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Browser.Puppeteer.Tests;

public class PuppeteerOptionsAndRegistrationTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(pair => $"Browser:{pair.Key}", pair => pair.Value)).Build();

    private static PuppeteerBrowserRenderer Renderer(PuppeteerOptions options) => new(Options.Create(options), NullLogger<PuppeteerBrowserRenderer>.Instance);

    [Fact]
    public void TheDefaults_AreTheSafeOnes()
    {
        PuppeteerOptions options = new();

        Assert.False(options.DisableSandbox);
        Assert.False(options.IgnoreCertificateErrors);
        Assert.False(options.AllowPrivateNetworks);
        Assert.Equal((4, 30, 60, 30, 200, 2, 20), (options.MaxConcurrentPages, options.WaitForPageSeconds, options.OperationTimeoutSeconds, options.NavigationTimeoutSeconds, options.RecycleAfterPages, options.LaunchAttempts, options.ShutdownTimeoutSeconds));
        Assert.Empty(options.AllowedHosts);
        Assert.Empty(options.ExtraArguments);
    }

    [Theory]
    [InlineData("MaxConcurrentPages", "0")]
    [InlineData("MaxConcurrentPages", "65")]
    [InlineData("WaitForPageSeconds", "0")]
    [InlineData("OperationTimeoutSeconds", "601")]
    [InlineData("NavigationTimeoutSeconds", "0")]
    [InlineData("RecycleAfterPages", "-1")]
    [InlineData("LaunchAttempts", "0")]
    [InlineData("ShutdownTimeoutSeconds", "-1")]
    public void AValueOutOfItsRange_IsAnErrorAtTheStart(string key, string value)
    {
        PuppeteerOptions options = new() { ExecutablePath = "/usr/bin/chromium" };
        IConfiguration configuration = Config((key, value));
        configuration.GetSection("Browser").Bind(options);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Renderer(options));

        Assert.Contains($"Browser:{key}", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheChromeIsRequired()
        => Assert.Contains("Browser:ExecutablePath", Assert.Throws<InvalidOperationException>(() => Renderer(new PuppeteerOptions())).Message, StringComparison.Ordinal);

    [Fact]
    public void TheArgumentsOfTheConstructorAreChecked()
    {
        Assert.Throws<ArgumentNullException>(() => new PuppeteerBrowserRenderer(null!, NullLogger<PuppeteerBrowserRenderer>.Instance));
        Assert.Throws<ArgumentNullException>(() => new PuppeteerBrowserRenderer(Options.Create(new PuppeteerOptions { ExecutablePath = "x" }), null!));
    }

    [Fact]
    public async Task TheRegistration_GivesOneRendererForTheContract_AndAReadinessCheck()
    {
        string executable = Path.GetTempFileName();
        try
        {
            ServiceCollection services = new();
            services.AddLogging().AddPuppeteerBrowser(Config(("ExecutablePath", executable), ("AllowedHosts:0", "*.example.com")));
            await using ServiceProvider provider = services.BuildServiceProvider();

            IBrowserRenderer renderer = provider.GetRequiredService<IBrowserRenderer>();
            HealthReport report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(entry => entry.Name == "browser");

            Assert.Same(renderer, provider.GetRequiredService<PuppeteerBrowserRenderer>());
            Assert.Equal(HealthStatus.Healthy, report.Status);
            Assert.Equal(0, provider.GetRequiredService<PuppeteerBrowserRenderer>().ActivePages);
        }
        finally
        {
            File.Delete(executable);
        }
    }

    [Fact]
    public async Task TheReadinessCheck_IsUnhealthy_WhenTheChromeIsNotThere()
    {
        ServiceCollection services = new();
        services.AddLogging().AddPuppeteerBrowser(Config(("ExecutablePath", "/nowhere/chrome")));
        await using ServiceProvider provider = services.BuildServiceProvider();

        HealthReport report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(entry => entry.Name == "browser");

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddPuppeteerBrowser(Config()));
        Assert.Throws<ArgumentNullException>(() => services.AddPuppeteerBrowser(null!));
    }

    [Fact]
    public async Task ARequestThatIsWrongOrNotAllowed_FailsBeforeAnyBrowserIsStarted()
    {
        await using PuppeteerBrowserRenderer renderer = Renderer(new PuppeteerOptions { ExecutablePath = "/nowhere/chrome" });

        await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.GetPageAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => renderer.GetPageAsync(new BrowserRequest()));
        await Assert.ThrowsAsync<BrowserBlockedException>(() => renderer.RenderPdfAsync(BrowserRequest.FromUrl(new Uri("http://127.0.0.1:9200/_search"))));
        await Assert.ThrowsAsync<BrowserBlockedException>(() => renderer.ScreenshotAsync(BrowserRequest.FromUrl(new Uri("http://169.254.169.254/latest/meta-data"))));
        await Assert.ThrowsAsync<ArgumentException>(() => renderer.ScreenshotAsync(BrowserRequest.FromHtml("<p>x</p>"), new ScreenshotOptions { Quality = 0 }));
    }

    [Fact]
    public async Task ARendererThatWasDisposed_RefusesCalls_AndDisposingTwiceIsHarmless()
    {
        PuppeteerBrowserRenderer renderer = Renderer(new PuppeteerOptions { ExecutablePath = "/nowhere/chrome", ShutdownTimeoutSeconds = 0 });

        await renderer.DisposeAsync();
        await renderer.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => renderer.GetPageAsync(BrowserRequest.FromHtml("<p>x</p>")));
    }

    [Fact]
    public async Task AChromeThatCannotBeStarted_IsABrowserException()
    {
        await using PuppeteerBrowserRenderer renderer = Renderer(new PuppeteerOptions { ExecutablePath = "/nowhere/chrome", LaunchAttempts = 2 });

        BrowserException error = await Assert.ThrowsAnyAsync<BrowserException>(() => renderer.GetPageAsync(BrowserRequest.FromHtml("<p>x</p>")));

        Assert.Contains("could not be rendered", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, renderer.ActivePages);
    }
}
