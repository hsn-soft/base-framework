#pragma warning disable CA1848, CA1873 // the tests call the logger the way a service does
using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.Logging;
using NLog;

namespace HsnSoft.Base.Logging.NLog.Tests;

public sealed class BaseNLogTests
{
    public sealed class Account
    {
        public string? Name { get; set; }

        [SensitiveData(SubstituteText = "***")]
        public string? Password { get; set; }
    }

    private static object? Property(LogEventInfo logEvent, string name) => logEvent.Properties.TryGetValue(name, out object? value) ? value : null;

    [Fact]
    public void TheTypeOfALog_IsTold_ByTheNamespaceOfTheApplication_AndWhatIsPersistentFollowsIt()
    {
        using Harness harness = new(options => options.AppNamespace = "Orders");

        harness.Logger("Orders.Checkout.OrderService").LogInformation("the application");
        harness.Logger("HsnSoft.Base.EventBus.EventDispatcher").LogInformation("the framework");
        harness.Logger("Polly.Retry").LogInformation("the platform, information");
        harness.Logger("Polly.Retry").LogWarning("the platform, warning");

        // the console sees all four; the platform's information stays there, as its level says
        Assert.Equal(4, harness.Console.Events.Count);
        Assert.Equal(["the application", "the framework", "the platform, warning"], harness.Persistent.Events.Select(logEvent => logEvent.Message));
        Assert.Equal(["AppLog", "FrameworkLog", "PlatformLog"], harness.Persistent.Events.Select(logEvent => Property(logEvent, "LogType")));
    }

    [Fact]
    public void DebugAndTrace_StayInTheConsole_AndTheConsoleOnlyLogsNeverLeaveIt()
    {
        using Harness harness = new();

        harness.Logger("Abc.Thing").LogDebug("debug");
        harness.Logger("Abc.Thing").LogTrace("trace");
        harness.Logger("ConsoleOnly.Startup").LogCritical("for the console");
        harness.Logger("Abc.Thing").LogError("error");

        Assert.Equal(4, harness.Console.Events.Count);
        Assert.Equal(["error"], harness.Persistent.Events.Select(logEvent => logEvent.Message));
        Assert.Contains("Startup | for the console", harness.Console.Lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void TheConsole_ShowsWhatLoggingLogLevelSays_PerCategory()
    {
        using Harness harness = new(options => options.ConsoleLevels = new LogLevelRules(Microsoft.Extensions.Logging.LogLevel.Information, [new("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)]));

        harness.Logger("Abc.Thing").LogInformation("shown");
        harness.Logger("Abc.Thing").LogDebug("hidden");
        harness.Logger("Microsoft.AspNetCore").LogInformation("hidden too");
        harness.Logger("Microsoft.AspNetCore").LogWarning("shown too");

        Assert.Equal(["shown", "shown too"], harness.Console.Events.Select(logEvent => logEvent.Message));
    }

    [Fact]
    public void ThePersistentLog_HasTheFieldsOfTheFramework_AndNoNoise()
    {
        using Harness harness = new(options => (options.AppNamespace, options.Version) = ("Abc", "1.2.3"), loggerName: "OrderService");
        Microsoft.Extensions.Logging.ILogger logger = harness.Logger("Abc.OrderService.Application.OrderAppService");

        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = "c1", ["RequestPath"] = "/orders", ["ConnectionId"] = "x" }))
        {
            logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, new EventId(7, "CONSUME_EVENT_SUCCESS"), "Order {OrderId} was made", 42);
        }

        LogEventInfo shaped = Assert.Single(harness.Persistent.Events);
        Assert.Equal("Order 42 was made", shaped.Message);
        Assert.Equal("c1", Property(shaped, "CorrelationId"));
        Assert.Equal(42, Property(shaped, "OrderId"));
        Assert.Equal("AppLog", Property(shaped, "LogType"));
        Assert.Equal("Args", Property(shaped, "MessageType"));
        Assert.Equal("CONSUME_EVENT_SUCCESS", Property(shaped, "Facility"));
        Assert.Equal("OrderService", Property(shaped, "LoggerName"));
        Assert.Equal("1.2.3", Property(shaped, "Version"));
        Assert.NotNull(Property(shaped, "Solution"));
        Assert.NotNull(Property(shaped, "Assembly"));
        Assert.All(["EventId", "EventName", "ConnectionId", "RequestPath"], name => Assert.False(shaped.Properties.ContainsKey(name), name));
    }

    [Fact]
    public void AnObject_IsOneFieldOfMaskedJson_AndTheLogIsStructured()
    {
        using Harness harness = new();

        harness.Logger("Abc.Thing").LogInformation("Signed in {@Account} as {Name}", new Account { Name = "alice", Password = "secret" }, "alice");

        LogEventInfo shaped = Assert.Single(harness.Persistent.Events);
        Assert.Equal("""{"Name":"alice","Password":"***"}""", Property(shaped, "Account"));
        Assert.Equal("Structured", Property(shaped, "MessageType"));
        Assert.Equal("""Signed in {"Name":"alice","Password":"***"} as alice""", shaped.Message);
        Assert.DoesNotContain("secret", harness.Console.Lines[0], StringComparison.Ordinal);
        Assert.Contains("***", harness.Console.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ALongText_IsCut_AndAFacilityThatIsAlreadyThere_IsKept()
    {
        using Harness harness = new(options => options.MaxPersistentFieldLength = 40);
        Microsoft.Extensions.Logging.ILogger logger = harness.Logger("Abc.Thing");

        using (logger.BeginScope(new Dictionary<string, object?> { ["Facility"] = "MY_FACILITY", ["LogType"] = "RequestLog" }))
        {
            logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, new EventId(1, "OTHER_NAME"), "{Body}", new string('x', 500));
        }

        LogEventInfo shaped = Assert.Single(harness.Persistent.Events);
        Assert.Equal(new string('x', 40) + "...(+460)", Property(shaped, "Body"));
        Assert.Equal(new string('x', 40) + "...(+460)", shaped.Message);
        Assert.Equal("MY_FACILITY", Property(shaped, "Facility"));
        Assert.Equal("RequestLog", Property(shaped, "LogType"));
    }

    [Fact]
    public void ADictionaryAndAUriAndAnException_AreWrittenAsTheyShould()
    {
        using Harness harness = new();

        harness.Logger("Abc.Thing").LogError(new InvalidOperationException("boom"), "Failed {Labels} at {Address}", new Dictionary<string, string> { ["a"] = "b" }, new Uri("http://x/y"));

        LogEventInfo shaped = Assert.Single(harness.Persistent.Events);
        Assert.Equal("""{"a":"b"}""", Property(shaped, "Labels"));
        Assert.Equal("http://x/y", Property(shaped, "Address"));
        Assert.Equal("Structured", Property(shaped, "MessageType"));
        Assert.Equal("boom", shaped.Exception!.Message);
        Assert.Contains("InvalidOperationException: boom", harness.Console.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void TheShapingWithoutALoggerName_TakesTheLogAsTheApplications()
    {
        using Harness harness = new(options => options.AppNamespace = "Abc");

        LogEventInfo shaped = harness.Shape.Shape(new LogEventInfo(global::NLog.LogLevel.Info, null, "no category"));

        Assert.Equal("AppLog", Property(shaped, "LogType"));
        Assert.Equal("no category", shaped.Message);
        Assert.Throws<ArgumentNullException>(() => harness.Shape.Shape(null!));
    }

    [Fact]
    public void TheConfigureCallback_CanAddATargetOfTheProduct()
    {
        CaptureTarget own = new();
        using Harness harness = new(configureNLog: configuration => configuration.AddRule(global::NLog.LogLevel.Warn, global::NLog.LogLevel.Fatal, own));

        harness.Logger("Abc.Thing").LogInformation("not for it");
        harness.Logger("Abc.Thing").LogWarning("for it");

        Assert.Equal(["for it"], own.Events.Select(logEvent => logEvent.Message));
    }
}
