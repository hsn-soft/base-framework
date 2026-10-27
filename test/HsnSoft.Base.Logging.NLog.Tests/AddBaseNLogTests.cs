#pragma warning disable CA1848, CA1873 // the tests call the logger the way a service does
using System.Net;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Common;

namespace HsnSoft.Base.Logging.NLog.Tests;

[Collection(ProcessWideCollection.Name)]
public sealed class AddBaseNLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hsn-nlog-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value)).Build();

    private static ServiceProvider Build(IConfiguration configuration, CaptureTarget? capture = null)
        => new ServiceCollection()
            .AddLogging(builder => builder.AddBaseNLog(configuration, "OrderService", capture is null ? null : nlog => nlog.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal, capture)))
            .AddBaseLogging()
            .BuildServiceProvider();

    [Fact]
    public void ThePersistentLogs_GoToTheFile_WhenGraylogIsOff_AndTheFileHasTheLineOfTheFramework()
    {
        string path = Path.Combine(_directory, "logs.txt");
        using (ServiceProvider provider = Build(Configuration(("PersistentLogger:FilePath", path), ("PersistentLogger:AppNamespace", "Abc"))))
        {
            Microsoft.Extensions.Logging.ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Abc.Orders.OrderService");
            logger.LogInformation("Order {OrderId} was made", 42);
            logger.LogError(new InvalidOperationException("boom"), "It failed");
        }

        string text = File.ReadAllText(path);
        Assert.Contains("INF] OrderService [Abc.Orders.OrderService] | Order 42 was made", text, StringComparison.Ordinal);
        Assert.Contains("ERR] OrderService [Abc.Orders.OrderService] | It failed", text, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException: boom", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheConsoleLogger_WritesToTheConsoleOnly_AndEveryOtherProviderIsReplaced()
    {
        string path = Path.Combine(_directory, "logs.txt");
        CaptureTarget everything = new();
        using (ServiceProvider provider = Build(Configuration(("PersistentLogger:FilePath", path)), everything))
        {
            provider.GetRequiredService<IAppConsoleLogger>().LogInformation("only for the console");
        }

        Assert.Single(everything.Events);
        Assert.StartsWith("ConsoleOnly.", everything.Events[0].LoggerName, StringComparison.Ordinal);
        Assert.False(File.Exists(path) && File.ReadAllText(path).Contains("only for the console", StringComparison.Ordinal));
    }

    [Fact]
    public void GraylogOn_NeedsAnAddress_AndWithOneTheServiceStarts()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Build(Configuration(("PersistentLogger:IsGrayLogActive", "true"))));
        Assert.Contains("PersistentLogger:GrayLog:Address", error.Message, StringComparison.Ordinal);

        using ServiceProvider provider = Build(Configuration(("PersistentLogger:IsGrayLogActive", "true"), ("PersistentLogger:GrayLog:Address", "localhost"), ("PersistentLogger:GrayLog:Port", "12201")));
        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>());
    }

    [Fact]
    public void TheSelfLog_ShowsTheErrorsOfNLogItself_WhenItIsOn()
    {
        global::NLog.LogLevel before = InternalLogger.LogLevel;
        bool toConsole = InternalLogger.LogToConsoleError;
        try
        {
            using ServiceProvider provider = Build(Configuration(("PersistentLogger:FilePath", Path.Combine(_directory, "logs.txt")), ("PersistentLogger:IsSelfLogActive", "true")));

            Assert.Equal(global::NLog.LogLevel.Warn, InternalLogger.LogLevel);
            Assert.True(InternalLogger.LogToConsoleError);
        }
        finally
        {
            InternalLogger.LogLevel = before;
            InternalLogger.LogToConsoleError = toConsole;
        }
    }

    [Fact]
    public void NullArguments_AreRejected_AndTheLoggerNameIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => NLogLoggingBuilderExtensions.AddBaseNLog(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddLogging(builder => builder.AddBaseNLog(null!)));
        Assert.Throws<ArgumentException>(() => BaseNLog.Create(new PersistentLoggerOptions(), " ", null));
        Assert.Throws<ArgumentNullException>(() => BaseNLog.Create(null!, "Host", null));
    }

    [Fact]
    public void TheLevelsOfNLog_AreTheLevelsOfMicrosoftExtensionsLogging_AndTheLinesAreTheSameAsSerilogs()
    {
        Assert.Equal(
            [Microsoft.Extensions.Logging.LogLevel.Trace, Microsoft.Extensions.Logging.LogLevel.Debug, Microsoft.Extensions.Logging.LogLevel.Information, Microsoft.Extensions.Logging.LogLevel.Warning, Microsoft.Extensions.Logging.LogLevel.Error, Microsoft.Extensions.Logging.LogLevel.Critical],
            new[] { global::NLog.LogLevel.Trace, global::NLog.LogLevel.Debug, global::NLog.LogLevel.Info, global::NLog.LogLevel.Warn, global::NLog.LogLevel.Error, global::NLog.LogLevel.Fatal }.Select(BaseNLog.ToLogLevel));

        string[] codes = [.. new[] { global::NLog.LogLevel.Trace, global::NLog.LogLevel.Debug, global::NLog.LogLevel.Info, global::NLog.LogLevel.Warn, global::NLog.LogLevel.Error, global::NLog.LogLevel.Fatal }
            .Select(level => NLogText.Console(new LogEventInfo(level, "Abc.X", "m"), "Host").Split(']')[0].Split(' ')[^1])];
        Assert.Equal(["TRC", "DBG", "INF", "WRN", "ERR", "FTL"], codes);
        Assert.Contains("] Host | m", NLogText.Console(new LogEventInfo(global::NLog.LogLevel.Info, "Abc.X", "m"), "Host"), StringComparison.Ordinal);
        Assert.Contains("] Startup | m", NLogText.Console(new LogEventInfo(global::NLog.LogLevel.Info, "ConsoleOnly.Startup", "m"), "Host"), StringComparison.Ordinal);
        Assert.Contains("] Host [Abc.X] | m", NLogText.File(new LogEventInfo(global::NLog.LogLevel.Info, "Abc.X", "m"), "Host"), StringComparison.Ordinal);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(Uri? Uri, string Body)> Requests { get; } = [];

        public int Failures { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add((request.RequestUri, string.Empty));
            }

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            lock (Requests)
            {
                Requests[^1] = (request.RequestUri, body);
                if (Failures > 0)
                {
                    Failures--;
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }

    [Fact]
    public void AGelfMessage_HasTheFieldsOfTheLog_WithTheNamesGraylogTakes()
    {
        using GelfHttpTarget target = new("graylog.internal", 12201, false, new HttpClient(new RecordingHandler()), "host-1");
        LogEventInfo logEvent = new(global::NLog.LogLevel.Error, "Abc.X", "it failed") { Exception = new InvalidOperationException("boom"), TimeStamp = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc) };
        logEvent.Properties["LogType"] = "AppLog";
        logEvent.Properties["Count"] = 3;
        logEvent.Properties["Ratio"] = 0.5;
        logEvent.Properties["Flag"] = true;
        logEvent.Properties["Nothing"] = null;
        logEvent.Properties["Id"] = Guid.Empty;
        logEvent.Properties["id"] = "reserved";
        logEvent.Properties["bad name!"] = "x";

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(target.Message(logEvent));
        System.Text.Json.JsonElement root = document.RootElement;

        Assert.Equal(("1.1", "host-1", "it failed", 3), (root.GetProperty("version").GetString(), root.GetProperty("host").GetString(), root.GetProperty("short_message").GetString(), root.GetProperty("level").GetInt32()));
        Assert.Contains("InvalidOperationException: boom", root.GetProperty("full_message").GetString(), StringComparison.Ordinal);
        Assert.Equal(1793095200, root.GetProperty("timestamp").GetDouble());
        Assert.Equal(("AppLog", 3, 0.5, true), (root.GetProperty("_LogType").GetString(), root.GetProperty("_Count").GetInt32(), root.GetProperty("_Ratio").GetDouble(), root.GetProperty("_Flag").GetBoolean()));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("_Nothing").ValueKind);
        Assert.Equal(Guid.Empty.ToString(), root.GetProperty("_Id").GetString());
        Assert.Equal("x", root.GetProperty("_bad_name_").GetString());
        Assert.False(root.TryGetProperty("_id", out _));
    }

    [Theory]
    [InlineData("Trace", 7)]
    [InlineData("Debug", 7)]
    [InlineData("Info", 6)]
    [InlineData("Warn", 4)]
    [InlineData("Error", 3)]
    [InlineData("Fatal", 2)]
    public void TheLevelOfAGelfMessage_IsTheSyslogLevel(string level, int syslog)
    {
        using GelfHttpTarget target = new("graylog.internal", 12201, false, new HttpClient(new RecordingHandler()));

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(target.Message(new LogEventInfo(global::NLog.LogLevel.FromString(level), "x", string.Empty)));

        Assert.Equal(syslog, document.RootElement.GetProperty("level").GetInt32());
        Assert.Equal("-", document.RootElement.GetProperty("short_message").GetString());
        Assert.False(document.RootElement.TryGetProperty("full_message", out _));
    }

    [Fact]
    public void AClientTheTargetMadeItself_IsDisposedWithIt()
    {
        GelfHttpTarget target = new("graylog.internal", 12201);

        Exception? failure = Record.Exception(() =>
        {
            target.Dispose();
            target.Dispose();
        });

        Assert.Null(failure);
    }

    [Theory]
    [InlineData("graylog.internal", false, "http://graylog.internal:12201/gelf")]
    [InlineData("graylog.internal", true, "https://graylog.internal:12201/gelf")]
    [InlineData("https://graylog.internal/", false, "https://graylog.internal:12201/gelf")]
    [InlineData("HTTP://graylog.internal", true, "http://graylog.internal:12201/gelf")]
    public void TheAddressOfGraylog_IsTheSchemeItSaysOrTheOneAskedFor(string address, bool useHttps, string expected)
        => Assert.Equal(expected, GelfHttpTarget.Endpoint(address, 12201, useHttps).ToString());

    [Fact]
    public void TheLogsAreSentToGraylogInTheBackground_AndAFailedOneIsTriedAgain()
    {
        RecordingHandler handler = new() { Failures = 1 };
        using GelfHttpTarget target = new("graylog.internal", 12201, false, new HttpClient(handler), "host-1");
        LogFactory factory = new() { Configuration = new global::NLog.Config.LoggingConfiguration() };
        factory.Configuration.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal, target);
        factory.ReconfigExistingLoggers();

        factory.GetLogger("Abc.X").Info("hello");
        factory.Flush(TimeSpan.FromSeconds(10));
        factory.Shutdown();

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal("http://graylog.internal:12201/gelf", request.Uri!.ToString()));
        Assert.Contains("\"short_message\":\"hello\"", handler.Requests[1].Body, StringComparison.Ordinal);
    }
}
