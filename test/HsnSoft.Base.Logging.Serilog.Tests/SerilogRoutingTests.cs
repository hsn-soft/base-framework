using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace HsnSoft.Base.Logging.Serilog.Tests;

public class SerilogRoutingTests
{
    [Theory]
    [InlineData(LogLevel.Trace, LogEventLevel.Verbose)]
    [InlineData(LogLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogLevel.Information, LogEventLevel.Information)]
    [InlineData(LogLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogLevel.Error, LogEventLevel.Error)]
    [InlineData(LogLevel.Critical, LogEventLevel.Fatal)]
    [InlineData(LogLevel.None, LogEventLevel.Fatal)]
    public void Levels_AreMapped(LogLevel level, LogEventLevel expected)
    {
        Assert.Equal(expected, SerilogRouting.ToSerilog(level));
    }

    [Fact]
    public void SourceContext_IsTheCategory_OrEmpty()
    {
        Assert.Equal("My.Category", SerilogRouting.SourceContext(Events.Create(LogEventLevel.Information, "My.Category")));
        Assert.Equal(string.Empty, SerilogRouting.SourceContext(Events.Create(LogEventLevel.Information)));
        LogEvent notText = new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplateParser().Parse("m"), [new LogEventProperty("SourceContext", new ScalarValue(5))]);
        Assert.Equal(string.Empty, SerilogRouting.SourceContext(notText));
    }

    [Theory]
    [InlineData("ConsoleOnly.ThreadPool", true)]
    [InlineData("ConsoleOnly.", true)]
    [InlineData("HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer", false)]
    [InlineData("MyConsoleOnly.X", false)]
    [InlineData("", false)]
    public void ConsoleOnlyCategories_AreRecognizedByTheirPrefix(string category, bool expected)
    {
        Assert.Equal(expected, SerilogRouting.IsConsoleOnly(Events.Create(LogEventLevel.Information, category)));
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, LogEventLevel.Verbose, true)]
    [InlineData(LogEventLevel.Debug, LogEventLevel.Information, false)]
    [InlineData(LogEventLevel.Information, LogEventLevel.Information, true)]
    [InlineData(LogEventLevel.Warning, LogEventLevel.Error, false)]
    public void Console_ShowsWhatIsAtLeastTheConfiguredLevel(LogEventLevel level, LogEventLevel minimum, bool expected)
    {
        Assert.Equal(expected, SerilogRouting.ForConsole(Events.Create(level), minimum));
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, "Any", false)]
    [InlineData(LogEventLevel.Debug, "Any", false)]
    [InlineData(LogEventLevel.Information, "Any", true)]
    [InlineData(LogEventLevel.Warning, "Any", true)]
    [InlineData(LogEventLevel.Fatal, "Any", true)]
    [InlineData(LogEventLevel.Information, "ConsoleOnly.Any", false)]
    [InlineData(LogEventLevel.Error, "ConsoleOnly.Any", false)]
    public void PersistentSink_NeverGetsDebugOrVerbose_NorTheConsoleLoggersLogs(LogEventLevel level, string category, bool expected)
    {
        Assert.Equal(expected, SerilogRouting.ForPersistent(Events.Create(level, category), LogEventLevel.Information));
    }

    [Theory]
    [InlineData(LogEventLevel.Information, "PlatformLog", false)]
    [InlineData(LogEventLevel.Warning, "PlatformLog", true)]
    [InlineData(LogEventLevel.Error, "PlatformLog", true)]
    [InlineData(LogEventLevel.Information, "AppLog", true)]
    [InlineData(LogEventLevel.Information, "FrameworkLog", true)]
    [InlineData(LogEventLevel.Information, "HttpRequest", true)]
    [InlineData(LogEventLevel.Information, "ConsumeStart", true)]
    public void TheInformationOfThePlatform_StaysInTheConsole_WhateverElseIsPersistent(LogEventLevel level, string logType, bool expected)
    {
        // by the time a log reaches a sink the enrichers have given it its LogType, and that is what says whether it is the platform's
        LogEvent logEvent = Events.Create(level, "Any.Category", new LogEventProperty("LogType", new ScalarValue(logType)));

        Assert.Equal(expected, SerilogRouting.ForPersistent(logEvent, LogEventLevel.Information, LogEventLevel.Warning));
    }

    [Fact]
    public void ALogWithoutAType_IsNotThePlatforms()
    {
        Assert.True(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Information, "Microsoft.X"), LogEventLevel.Information, LogEventLevel.Warning));
    }

    [Fact]
    public void PersistentSink_CanBeStricter()
    {
        Assert.False(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Information, "Any"), LogEventLevel.Warning));
        Assert.True(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Error, "Any"), LogEventLevel.Warning));
    }

    [Fact]
    public void LoggerName_IsTheConsoleLoggersName_OrTheHosts()
    {
        Assert.Equal("ThreadPool", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information, "ConsoleOnly.ThreadPool"), "Host"));
        Assert.Equal("Host", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information, "Some.Library"), "Host"));
        Assert.Equal("Host", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information), "Host"));
    }
}
