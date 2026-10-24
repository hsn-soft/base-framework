using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace HsnSoft.Base.Logging.Serilog.Tests;

[Collection(ProcessWideCollection.Name)]
public sealed class BaseSerilogTests : IDisposable
{
    private static readonly string[] s_dependencyCategories = ["System", "Microsoft", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Npgsql", "HsnSoft.Base.MongoDB", "MongoDB"];

    private readonly CaptureSink _console = new();
    private readonly CaptureSink _persistent = new();
    private readonly List<Logger> _loggers = [];

    public void Dispose()
    {
        foreach (Logger logger in _loggers)
        {
            logger.Dispose();
        }
    }

    public sealed class Account
    {
        public string? Name { get; set; }

        [SensitiveData]
        public string? Password { get; set; }
    }

    private Logger Create(Action<FrameworkLoggerOptions>? configure = null, Action<LoggerConfiguration>? configureSerilog = null, string loggerName = "Host")
    {
        FrameworkLoggerOptions options = new();
        configure?.Invoke(options);
        SerilogSinks sinks = new(_console, _persistent, null);
        Logger logger = BaseSerilog.Create(options, loggerName, configureSerilog, sinks).Logger;
        _loggers.Add(logger);
        return logger;
    }

    [Fact]
    public void DebugAndVerbose_StayInTheConsole()
    {
        ILogger logger = Create();

        logger.Verbose("verbose");
        logger.Debug("debug");

        Assert.Equal(["verbose", "debug"], _console.Messages);
        Assert.Empty(_persistent.Events);
    }

    [Fact]
    public void InformationAndAbove_GoToTheConsoleAndTheConsoleOnlyTheyArePersistent()
    {
        ILogger logger = Create();

        logger.Information("information");
        logger.Warning("warning");
        logger.Error("error");
        logger.Fatal("fatal");

        Assert.Equal(["information", "warning", "error", "fatal"], _console.Messages);
        Assert.Equal(["information", "warning", "error", "fatal"], _persistent.Messages);
    }

    [Fact]
    public void ConsoleOnlyCategory_IsNotPersistent_AndShowsItsOwnName()
    {
        ILogger logger = Create();

        logger.ForContext(Constants.SourceContextPropertyName, "ConsoleOnly.ThreadPool").Information("seen by the pod only");

        LogEvent shown = Assert.Single(_console.Events);
        Assert.Equal("ThreadPool", shown.Properties["LoggerName"].ToString().Trim('"'));
        Assert.Empty(_persistent.Events);
    }

    [Fact]
    public void ConfiguredLevel_DecidesWhatAnythingWrites()
    {
        ILogger logger = Create(options => options.LogLevel = LogLevel.Warning);

        logger.Debug("debug");
        logger.Information("information");
        logger.Warning("warning");

        Assert.Equal(["warning"], _console.Messages);
        Assert.Equal(["warning"], _persistent.Messages);
    }

    [Fact]
    public void PersistentLevelBelowInformation_IsIgnored()
    {
        ILogger logger = Create(options => options.PersistentMinimumLevel = LogLevel.Trace);

        logger.Debug("debug");
        logger.Information("information");

        Assert.Equal(["information"], _persistent.Messages);
    }

    [Fact]
    public void StricterPersistentLevel_IsKept()
    {
        ILogger logger = Create(options => options.PersistentMinimumLevel = LogLevel.Error);

        logger.Warning("warning");
        logger.Error("error");

        Assert.Equal(["warning", "error"], _console.Messages);
        Assert.Equal(["error"], _persistent.Messages);
    }

    [Fact]
    public void LibrariesAreQuieter_ThanTheApplication()
    {
        ILogger logger = Create();

        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting").Verbose("hidden");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting").Debug("shown");
        logger.ForContext(Constants.SourceContextPropertyName, "HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer").Verbose("application");

        Assert.Equal(["shown", "application"], _console.Messages);
    }

    [Theory]
    [InlineData(LogLevel.Trace, LogLevel.Debug)]
    [InlineData(LogLevel.Debug, LogLevel.Information)]
    [InlineData(LogLevel.Information, LogLevel.Warning)]
    [InlineData(LogLevel.Error, LogLevel.Warning)]
    public void LibraryLevel_FollowsTheApplicationsLevel(LogLevel configured, LogLevel expected)
    {
        FrameworkLoggerOptions options = new() { LogLevel = configured };

        Dictionary<string, LogLevel> levels = BaseSerilog.Overrides(options).ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.All(s_dependencyCategories, category => Assert.Equal(expected, levels[category]));
    }

    [Fact]
    public void ConfiguredOverrides_BeatTheDefaults_AndAddCategories()
    {
        ILogger logger = Create(options =>
        {
            options.LevelOverrides["Microsoft"] = LogLevel.Error;
            options.LevelOverrides["MyApp.Noisy"] = LogLevel.Warning;
        });

        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.Extensions.Http").Warning("hidden");
        logger.ForContext(Constants.SourceContextPropertyName, "MyApp.Noisy.Part").Information("hidden too");
        logger.ForContext(Constants.SourceContextPropertyName, "MyApp.Noisy.Part").Warning("shown");

        Assert.Equal(["shown"], _console.Messages);
    }

    [Fact]
    public void Logs_CarryTheSolutionTheAssemblyAndTheHostsName()
    {
        ILogger logger = Create(loggerName: "OrderService");

        logger.Information("hello");

        LogEvent logEvent = Assert.Single(_console.Events);
        Assert.True(logEvent.Properties.ContainsKey("Solution"));
        Assert.True(logEvent.Properties.ContainsKey("Assembly"));
        Assert.Equal("OrderService", logEvent.Properties["LoggerName"].ToString().Trim('"'));
    }

    [Fact]
    public void Logs_CarryTheVersionOfTheBuild()
    {
        ILogger logger = Create(configure: options => options.Version = "2610241530-sha-ab12cd3");

        logger.Information("hello");

        Assert.Equal("2610241530-sha-ab12cd3", Assert.Single(_console.Events).Properties["Version"].ToString().Trim('"'));
    }

    [Fact]
    public void ObjectsLoggedWithTheAtSign_HaveTheirSensitiveDataMasked()
    {
        ILogger logger = Create();

        logger.Information("account {@Account}", new Account { Name = "alice", Password = "hunter2" });

        LogEvent logEvent = Assert.Single(_console.Events);
        string rendered = logEvent.Properties["Account"].ToString();
        Assert.Contains("alice", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", rendered, StringComparison.Ordinal);
        Assert.Contains("*****", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductCanAddToTheConfiguration()
    {
        CaptureSink extra = new();
        ILogger logger = Create(configureSerilog: configuration => configuration.WriteTo.Sink(extra));

        logger.Debug("everything");

        Assert.Equal(["everything"], extra.Messages);
    }

    [Fact]
    public void SelfLog_WhenActive_GoesToTheErrorOutput_NotToASink()
    {
        TextWriter originalError = Console.Error;
        using StringWriter captured = new();
        Console.SetError(captured);
        try
        {
            Create(options => options.IsSelfLogActive = true);

            SelfLog.WriteLine("the sink could not reach graylog");

            string output = captured.ToString();
            Assert.Contains("[Serilog SelfLog]", output, StringComparison.Ordinal);
            Assert.Contains("the sink could not reach graylog", output, StringComparison.Ordinal);
            Assert.Empty(_persistent.Events);
        }
        finally
        {
            SelfLog.Disable();
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void NullOptions_AndABlankName_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => BaseSerilog.Create(null!, "Host", null));
        Assert.Throws<ArgumentException>(() => BaseSerilog.Create(new FrameworkLoggerOptions(), " ", null));
    }
}
