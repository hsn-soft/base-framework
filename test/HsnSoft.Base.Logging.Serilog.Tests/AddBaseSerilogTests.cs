#pragma warning disable CA1848 // the tests call the logger the way a service does
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog.Tests;

/// <summary>The whole way: a service's loggers, the configuration section and a real persistent sink (a file).</summary>
[Collection(ProcessWideCollection.Name)]
public sealed class AddBaseSerilogTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("hsn-log-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PersistentLogger:FilePath"] = Path.Combine(_directory, "service.txt"),
                ["PersistentLogger:LogLevel"] = "Verbose",
            })
            .AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value))
            .Build();

    private static ServiceProvider Build(IConfiguration configuration, Action<LoggerConfiguration>? configure = null, bool replaceGlobalLogger = false)
    {
        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddBaseSerilog(configuration, "OrderService", configure, replaceGlobalLogger));
        services.AddBaseLogging();
        return services.BuildServiceProvider();
    }

    private string PersistentText()
    {
        string file = Assert.Single(Directory.GetFiles(_directory, "service*.txt"));
        using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void WhatTheServiceLogs_IsPersistentFromInformationUp_AndTheConsoleLoggerStaysInTheConsole()
    {
        CaptureSink everything = new();
        ServiceProvider provider = Build(Configuration(), configure => configure.WriteTo.Sink(everything));
        ILogger<AddBaseSerilogTests> logger = provider.GetRequiredService<ILogger<AddBaseSerilogTests>>();
        IAppConsoleLogger console = provider.GetRequiredService<IAppConsoleLogger>();

        logger.LogTrace("trace message");
        logger.LogDebug("debug message");
        logger.LogInformation("information message");
        logger.LogWarning("warning message");
        console.LogInformation("console-only message");
        console.LogError("console-only error");
        logger.LogError("order failure {Order}", 7);
        provider.Dispose();

        // everything reached the logging pipeline, Debug and Trace included (the default filter of Microsoft.Extensions.Logging is out of the way)
        string[] all = [.. everything.Messages];
        Assert.Contains("trace message", all);
        Assert.Contains("debug message", all);

        string persistent = PersistentText();
        Assert.Contains("information message", persistent, StringComparison.Ordinal);
        Assert.Contains("warning message", persistent, StringComparison.Ordinal);
        Assert.Contains("order failure 7", persistent, StringComparison.Ordinal);
        Assert.DoesNotContain("trace message", persistent, StringComparison.Ordinal);
        Assert.DoesNotContain("debug message", persistent, StringComparison.Ordinal);
        Assert.DoesNotContain("console-only", persistent, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguredLevel_IsHonoured()
    {
        ServiceProvider provider = Build(Configuration(("PersistentLogger:LogLevel", "Error")));
        ILogger<AddBaseSerilogTests> logger = provider.GetRequiredService<ILogger<AddBaseSerilogTests>>();

        logger.LogWarning("a warning");
        logger.LogError("an error");
        provider.Dispose();

        string persistent = PersistentText();
        Assert.DoesNotContain("a warning", persistent, StringComparison.Ordinal);
        Assert.Contains("an error", persistent, StringComparison.Ordinal);
    }

    [Fact]
    public void WithGraylog_ThePersistentSinkIsRefreshedByAHostedService()
    {
        using ServiceProvider provider = Build(Configuration(
            ("PersistentLogger:IsGrayLogActive", "true"),
            ("PersistentLogger:GrayLog:Address", "http://127.0.0.1"),
            ("PersistentLogger:GrayLog:Port", "1")));

        Assert.Contains(provider.GetServices<IHostedService>(), service => service is GraylogSinkRefreshService);
    }

    [Fact]
    public void WithoutGraylog_NothingIsRefreshed()
    {
        using ServiceProvider provider = Build(Configuration());

        Assert.DoesNotContain(provider.GetServices<IHostedService>(), service => service is GraylogSinkRefreshService);
    }

    [Fact]
    public void Graylog_WithoutAnAddress_StopsTheStartup()
    {
        Assert.Throws<InvalidOperationException>(() => Build(Configuration(("PersistentLogger:IsGrayLogActive", "true"))));
    }

    [Fact]
    public void GlobalLogger_IsReplacedUnlessTheProductSaysNo()
    {
        global::Serilog.ILogger before = Log.Logger;
        try
        {
            Build(Configuration()).Dispose();
            Assert.Same(before, Log.Logger);

            using (Build(Configuration(), replaceGlobalLogger: true))
            {
                Assert.NotSame(before, Log.Logger);
            }
        }
        finally
        {
            Log.Logger = before;
        }
    }

    [Fact]
    public void SerilogsOwnLevels_AreAcceptedInTheConfiguration()
    {
        ServiceProvider provider = Build(Configuration(("PersistentLogger:LogLevel", "Fatal")));
        ILogger<AddBaseSerilogTests> logger = provider.GetRequiredService<ILogger<AddBaseSerilogTests>>();

        logger.LogError("not fatal");
        logger.LogCritical("fatal");
        provider.Dispose();

        string persistent = PersistentText();
        Assert.DoesNotContain("not fatal", persistent, StringComparison.Ordinal);
        Assert.Contains("fatal", persistent, StringComparison.Ordinal);
        Assert.Equal(LogEventLevel.Fatal, SerilogRouting.ToSerilog(LogLevel.Critical));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        ServiceCollection services = new();

        services.AddLogging(builder =>
        {
            Assert.Throws<ArgumentNullException>(() => SerilogLoggingBuilderExtensions.AddBaseSerilog(null!, Configuration()));
            Assert.Throws<ArgumentNullException>(() => builder.AddBaseSerilog(null!));
        });
    }
}
