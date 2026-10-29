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

    private Logger Create(Action<PersistentLoggerOptions>? configure = null, Action<LoggerConfiguration>? configureSerilog = null, string loggerName = "Host")
    {
        // what is not about the level of the console sees everything in it (Logging:LogLevel is tested below)
        PersistentLoggerOptions options = new() { ConsoleLevels = new LogLevelRules(LogLevel.Trace) };
        configure?.Invoke(options);
        SerilogSinks sinks = new(_console, _persistent, null);
        Logger logger = BaseSerilog.Create(options, loggerName, configureSerilog, sinks).Logger;
        _loggers.Add(logger);
        return logger;
    }

    [Fact]
    public void TheTypeOfALog_IsTold_ByTheNamespaceOfTheApplication_AndWhatIsPersistentFollowsIt()
    {
        ILogger logger = Create(options => options.AppNamespace = "Orders");

        logger.ForContext(Constants.SourceContextPropertyName, "Orders.Checkout.OrderService").Information("the application");
        logger.ForContext(Constants.SourceContextPropertyName, "HsnSoft.Base.EventBus.EventDispatcher").Information("the framework");
        logger.ForContext(Constants.SourceContextPropertyName, "Polly.Retry").Information("the platform, information");
        logger.ForContext(Constants.SourceContextPropertyName, "Polly.Retry").Warning("the platform, warning");

        // the console sees all four; the platform's information stays there, as its level says
        Assert.Equal(4, _console.Events.Count);
        Assert.Equal(["the application", "the framework", "the platform, warning"], _persistent.Messages);
        Assert.Equal(["AppLog", "FrameworkLog", "PlatformLog"], _persistent.Events.Select(logEvent => ((ScalarValue)logEvent.Properties["LogType"]).Value));
    }

    [Fact]
    public void WithoutAnAppNamespace_ItIsTheFirstPartOfTheAssemblyName()
    {
        string solution = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
        ILogger logger = Create();

        logger.ForContext(Constants.SourceContextPropertyName, $"{solution}.Orders.OrderService").Information("the application");

        Assert.Equal("AppLog", ((ScalarValue)Assert.Single(_persistent.Events).Properties["LogType"]).Value);
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
    public void TheConsole_IsToldByLoggingLogLevel_ADefaultAndALevelPerCategory()
    {
        ILogger logger = Create(options => options.ConsoleLevels = new LogLevelRules(LogLevel.Warning, [new("Microsoft.AspNetCore", LogLevel.Error), new("Orders", LogLevel.Debug)]));

        logger.ForContext(Constants.SourceContextPropertyName, "Some.Library").Information("hidden, below the default");
        logger.ForContext(Constants.SourceContextPropertyName, "Some.Library").Warning("shown, the default");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Routing").Warning("hidden, the category wants Error");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Routing").Error("shown, error");
        logger.ForContext(Constants.SourceContextPropertyName, "Orders.Checkout").Debug("shown, the application asks for Debug");

        Assert.Equal(["shown, the default", "shown, error", "shown, the application asks for Debug"], _console.Messages);
    }

    [Fact]
    public void TheConsoleLevel_AndThePersistentLevel_AreIndependent()
    {
        // the console shows only errors; what is kept is still Information and above
        ILogger logger = Create(options => options.ConsoleLevels = new LogLevelRules(LogLevel.Error));

        logger.Information("information");
        logger.Error("error");

        Assert.Equal(["error"], _console.Messages);
        Assert.Equal(["information", "error"], _persistent.Messages);
    }

    [Fact]
    public void TheConsole_CanBeSilencedForACategory_WithNone()
    {
        ILogger logger = Create(options => options.ConsoleLevels = new LogLevelRules(LogLevel.Information, [new("Chatty", LogLevel.None)]));

        logger.ForContext(Constants.SourceContextPropertyName, "Chatty.Library").Fatal("silent");
        logger.Information("shown");

        Assert.Equal(["shown"], _console.Messages);
    }

    [Fact]
    public void ThePersistentLevel_IsWhatPersistentLoggerLogLevelSays_AndNeverBelowInformation()
    {
        ILogger stricter = Create(options => options.LogLevel = LogLevel.Error);
        stricter.Warning("warning");
        stricter.Error("error");

        Assert.Equal(["warning", "error"], _console.Messages);
        Assert.Equal(["error"], _persistent.Messages);
    }

    [Fact]
    public void PersistentLevelBelowInformation_IsIgnored()
    {
        ILogger logger = Create(options => options.LogLevel = LogLevel.Trace);

        logger.Debug("debug");
        logger.Information("information");

        Assert.Equal(["debug", "information"], _console.Messages);
        Assert.Equal(["information"], _persistent.Messages);
    }

    [Fact]
    public void PersistentOverrides_ArePerCategory_AndDoNotTouchTheConsole()
    {
        ILogger logger = Create(options =>
        {
            options.AppNamespace = "Orders";
            options.LevelOverrides["Orders.Noisy"] = LogLevel.Warning;
        });

        logger.ForContext(Constants.SourceContextPropertyName, "Orders.Noisy.Part").Information("not kept");
        logger.ForContext(Constants.SourceContextPropertyName, "Orders.Noisy.Part").Warning("kept");
        logger.ForContext(Constants.SourceContextPropertyName, "Orders.Other").Information("kept too");

        Assert.Equal(3, _console.Events.Count);
        Assert.Equal(["kept", "kept too"], _persistent.Messages);
    }

    [Fact]
    public void ThePlatformsInformation_IsNotKept_UnlessItsCategoryIsNamed()
    {
        ILogger logger = Create(options => options.LevelOverrides["Microsoft.AspNetCore.Hosting"] = LogLevel.Information);

        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Routing").Information("platform, not kept");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics").Information("platform, named, kept");

        Assert.Equal(["platform, named, kept"], _persistent.Messages);
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
        Assert.Throws<ArgumentException>(() => BaseSerilog.Create(new PersistentLoggerOptions(), " ", null));
    }
}
