using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Tests;

public class BaseHostingTests
{
    private static WebApplicationBuilder Builder(Dictionary<string, string?>? settings = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        return builder;
    }

    [Fact]
    public void TheLimitsOfKestrel_AreTheOnesOfTheSettings_AndWhatIsNotSetKeepsTheDefault()
    {
        WebApplicationBuilder builder = Builder(new()
        {
            ["Hosting:Kestrel:MaxRequestBodySize"] = "12345",
            ["Hosting:Kestrel:MaxRequestBufferSize"] = "2222",
            ["Hosting:Kestrel:MaxRequestHeadersTotalSize"] = "4444",
            ["Hosting:Kestrel:MaxRequestHeaderCount"] = "7",
            ["Hosting:Kestrel:RequestHeadersTimeout"] = "00:00:11",
            ["Hosting:Kestrel:KeepAliveTimeout"] = "00:00:07",
            ["Hosting:Kestrel:MaxConcurrentConnections"] = "500",
        });
        builder.AddBaseHosting();

        using WebApplication app = builder.Build();
        KestrelServerLimits limits = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.Limits;

        Assert.Equal(12345, limits.MaxRequestBodySize);
        Assert.Equal(2222, limits.MaxRequestBufferSize);
        Assert.Equal(4444, limits.MaxRequestHeadersTotalSize);
        Assert.Equal(7, limits.MaxRequestHeaderCount);
        Assert.Equal(TimeSpan.FromSeconds(11), limits.RequestHeadersTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), limits.KeepAliveTimeout);
        Assert.Equal(500, limits.MaxConcurrentConnections);
    }

    [Fact]
    public void WithNoLimitsInTheSettings_KestrelKeepsItsDefaults()
    {
        WebApplicationBuilder builder = Builder();
        builder.AddBaseHosting();

        using WebApplication app = builder.Build();
        KestrelServerLimits limits = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.Limits;

        Assert.Equal(30_000_000, limits.MaxRequestBodySize);
        Assert.Equal(1_048_576, limits.MaxRequestBufferSize);
        Assert.Null(limits.MaxConcurrentConnections);
    }

    [Fact]
    public void ABodyLimitOfZero_MeansNoLimit()
    {
        WebApplicationBuilder builder = Builder(new() { ["Hosting:Kestrel:MaxRequestBodySize"] = "0" });
        builder.AddBaseHosting();

        using WebApplication app = builder.Build();

        Assert.Null(app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.Limits.MaxRequestBodySize);
    }

    [Fact]
    public void TheTimeToStop_IsTheOneOfTheSettings_ThirtySecondsByDefault()
    {
        WebApplicationBuilder withSetting = Builder(new() { ["Hosting:ShutdownTimeoutSeconds"] = "12" });
        withSetting.AddBaseHosting();
        WebApplicationBuilder byDefault = Builder();
        byDefault.AddBaseHosting();

        using WebApplication first = withSetting.Build();
        using WebApplication second = byDefault.Build();

        Assert.Equal(TimeSpan.FromSeconds(12), first.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), second.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
    }

    [Fact]
    public void TheProxiesOfTheSettings_AreTheOnesBelieved()
    {
        WebApplicationBuilder builder = Builder(new()
        {
            ["Hosting:ForwardedHeaders:KnownProxies:0"] = "10.0.0.4",
            ["Hosting:ForwardedHeaders:KnownNetworks:0"] = "10.244.0.0/16",
            ["Hosting:ForwardedHeaders:ForwardLimit"] = "2",
        });
        builder.AddBaseHosting();

        using WebApplication app = builder.Build();
        ForwardedHeadersOptions options = app.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Contains(IPAddress.Parse("10.0.0.4"), options.KnownProxies);
        Assert.Contains(System.Net.IPNetwork.Parse("10.244.0.0/16"), options.KnownIPNetworks);
        Assert.Equal(2, options.ForwardLimit);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost, options.ForwardedHeaders);
    }

    private static async Task<string> CallerAsync(Dictionary<string, string?> settings, IPAddress remote)
    {
        WebApplicationBuilder builder = Builder(settings);
        builder.WebHost.UseTestServer();
        builder.AddBaseHosting();
        await using WebApplication app = builder.Build();
        app.UseBaseForwardedHeaders();
        app.Run(context => context.Response.WriteAsync($"{context.Connection.RemoteIpAddress}|{context.Request.Scheme}"));
        await app.StartAsync();

        HttpContext context = await app.GetTestServer().SendAsync(request =>
        {
            request.Connection.RemoteIpAddress = remote;
            request.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
            request.Request.Headers["X-Forwarded-Proto"] = "https";
        });
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task AProxyThatIsKnown_IsBelieved_AndAnotherIsNot()
    {
        var settings = new Dictionary<string, string?> { ["Hosting:ForwardedHeaders:KnownNetworks:0"] = "10.244.0.0/16" };

        Assert.Equal("203.0.113.9|https", await CallerAsync(settings, IPAddress.Parse("10.244.1.5")));
        Assert.Equal("198.51.100.7|http", await CallerAsync(settings, IPAddress.Parse("198.51.100.7")));
    }

    [Fact]
    public async Task TheHeaders_AreNotRead_WhenTheSettingsSwitchThemOff()
    {
        var settings = new Dictionary<string, string?> { ["Hosting:ForwardedHeaders:Enabled"] = "false", ["Hosting:ForwardedHeaders:KnownNetworks:0"] = "10.244.0.0/16" };

        Assert.Equal("10.244.1.5|http", await CallerAsync(settings, IPAddress.Parse("10.244.1.5")));
    }

    private sealed class Memory : ILoggerProvider
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose()
        {
        }

        private sealed class Logger(Memory owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => owner.Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    private static WebApplicationBuilder Runnable(Memory memory, bool stopAtOnce = true)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddProvider(memory);
        if (stopAtOnce)
        {
            builder.Services.AddHostedService<StopAtOnce>();
        }

        return builder;
    }

    private sealed class StopAtOnce(IHostApplicationLifetime lifetime) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            lifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task AHostThatRunsAndStops_ReturnsZero_AndConfiguresTheApplication()
    {
        var memory = new Memory();
        WebApplicationBuilder builder = Runnable(memory);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Scheduler:ApiKey"] = "set" });
        builder.Services.RequireBaseSettings("Scheduler:ApiKey");
        bool configured = false;

        int code = await builder.RunBaseAsync((Action<WebApplication>)(_ => configured = true));

        Assert.Equal(0, code);
        Assert.True(configured);
    }

    [Fact]
    public async Task MissingSettings_StopTheStart_WithOneCriticalLogThatNamesThemAll_AndNoValue()
    {
        var memory = new Memory();
        WebApplicationBuilder builder = Runnable(memory);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Present:Key"] = "secret-value", ["Blank:Key"] = " " });
        builder.Services.RequireBaseSettings("Scheduler:ApiKey", "Present:Key");
        builder.Services.RequireBaseSettings("Blank:Key", "scheduler:apikey");
        bool configured = false;

        int code = await builder.RunBaseAsync((Action<WebApplication>)(_ => configured = true));

        Assert.Equal(1, code);
        Assert.False(configured);
        (LogLevel Level, string Message, Exception? Exception) entry = Assert.Single(memory.Entries, item => item.Level == LogLevel.Critical);
        Assert.Contains("Scheduler:ApiKey, Blank:Key", entry.Exception!.Message);
        Assert.DoesNotContain("secret-value", entry.Exception.Message);
        Assert.DoesNotContain("Present:Key", entry.Exception.Message);
    }

    [Fact]
    public async Task AConfigurationThatAwaits_IsAwaitedBeforeTheHostRuns()
    {
        WebApplicationBuilder builder = Runnable(new Memory());
        bool done = false;

        int code = await builder.RunBaseAsync(async _ =>
        {
            await Task.Delay(50);
            done = true;
        });

        Assert.Equal(0, code);
        Assert.True(done);
    }

    [Fact]
    public async Task AFailureOfTheConfiguration_IsLoggedAsCritical_AndReturnsOne()
    {
        var memory = new Memory();
        WebApplicationBuilder builder = Runnable(memory);

        int code = await builder.RunBaseAsync((Action<WebApplication>)(_ => throw new InvalidOperationException("the pipeline is wrong")));

        Assert.Equal(1, code);
        Assert.Contains(memory.Entries, item => item.Level == LogLevel.Critical && item.Exception!.Message == "the pipeline is wrong");
    }

    private sealed class NeedsAMissingService(IDisposable missing)
    {
        public IDisposable Missing { get; } = missing;
    }

    [Fact]
    public async Task AFailureBeforeTheHostIsBuilt_GoesToTheStandardError_AndReturnsOne()
    {
        WebApplicationBuilder builder = Runnable(new Memory(), stopAtOnce: false);
        builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = true);
        builder.Services.AddSingleton<NeedsAMissingService>();
        TextWriter previous = Console.Error;
        var error = new StringWriter();
        Console.SetError(error);
        try
        {
            int code = await builder.RunBaseAsync((Action<WebApplication>)(_ => { }));

            Assert.Equal(1, code);
            Assert.Contains("The host could not be built", error.ToString());
        }
        finally
        {
            Console.SetError(previous);
        }
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => BaseHostingExtensions.AddBaseHosting(null!));
        Assert.Throws<ArgumentNullException>(() => BaseHostingExtensions.UseBaseForwardedHeaders(null!));
        Assert.Throws<ArgumentNullException>(() => BaseHostingExtensions.RequireBaseSettings(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().RequireBaseSettings(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => BaseHostingExtensions.RunBaseAsync(null!, (Action<WebApplication>)(_ => { })));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Builder().RunBaseAsync((Action<WebApplication>)null!));
    }
}
