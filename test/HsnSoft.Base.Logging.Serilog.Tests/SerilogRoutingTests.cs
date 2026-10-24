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
    [InlineData(LogEventLevel.Verbose, LogLevel.Trace, true)]
    [InlineData(LogEventLevel.Debug, LogLevel.Information, false)]
    [InlineData(LogEventLevel.Information, LogLevel.Information, true)]
    [InlineData(LogEventLevel.Warning, LogLevel.Error, false)]
    [InlineData(LogEventLevel.Fatal, LogLevel.Critical, true)]
    [InlineData(LogEventLevel.Fatal, LogLevel.None, false)]
    public void Console_ShowsWhatIsAtLeastTheLevelOfItsCategory(LogEventLevel level, LogLevel minimum, bool expected)
    {
        Assert.Equal(expected, SerilogRouting.ForConsole(Events.Create(level, "Any.Category"), new LogLevelRules(minimum)));
    }

    [Fact]
    public void Console_TakesTheLevelOfTheCategory_AndOfTheDefaultForTheOthers()
    {
        LogLevelRules rules = new(LogLevel.Warning, [new("Microsoft.AspNetCore", LogLevel.Error)]);

        Assert.False(SerilogRouting.ForConsole(Events.Create(LogEventLevel.Warning, "Microsoft.AspNetCore.Routing"), rules));
        Assert.True(SerilogRouting.ForConsole(Events.Create(LogEventLevel.Error, "Microsoft.AspNetCore.Routing"), rules));
        Assert.True(SerilogRouting.ForConsole(Events.Create(LogEventLevel.Warning, "Other"), rules));
        // a log without a category has the default's level
        Assert.False(SerilogRouting.ForConsole(Events.Create(LogEventLevel.Information), rules));
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
        Assert.Equal(expected, SerilogRouting.ForPersistent(Events.Create(level, category), new PersistentLevelRules(new PersistentLoggerOptions())));
    }

    [Theory]
    [InlineData(LogEventLevel.Information, "PlatformLog", false)]
    [InlineData(LogEventLevel.Warning, "PlatformLog", true)]
    [InlineData(LogEventLevel.Error, "PlatformLog", true)]
    [InlineData(LogEventLevel.Information, "AppLog", true)]
    [InlineData(LogEventLevel.Information, "SharedLog", true)]
    [InlineData(LogEventLevel.Information, "FrameworkLog", true)]
    [InlineData(LogEventLevel.Information, "HttpRequest", true)]
    [InlineData(LogEventLevel.Information, "ConsumeStart", true)]
    public void TheInformationOfThePlatform_StaysInTheConsole_WhateverElseIsPersistent(LogEventLevel level, string logType, bool expected)
    {
        // by the time a log reaches a sink the enrichers have given it its LogType, and that is what says whether it is the platform's
        LogEvent logEvent = Events.Create(level, "Any.Category", new LogEventProperty("LogType", new ScalarValue(logType)));

        Assert.Equal(expected, SerilogRouting.ForPersistent(logEvent, new PersistentLevelRules(new PersistentLoggerOptions())));
    }

    [Fact]
    public void ALogWithoutAType_IsNotThePlatforms()
    {
        Assert.True(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Information, "Microsoft.X"), new PersistentLevelRules(new PersistentLoggerOptions())));
    }

    [Fact]
    public void PersistentSink_CanBeStricter()
    {
        PersistentLevelRules rules = new(new PersistentLoggerOptions { LogLevel = LogLevel.Warning });

        Assert.False(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Information, "Any"), rules));
        Assert.True(SerilogRouting.ForPersistent(Events.Create(LogEventLevel.Error, "Any"), rules));
    }

    [Fact]
    public void LoggerName_IsTheConsoleLoggersName_OrTheHosts()
    {
        Assert.Equal("ThreadPool", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information, "ConsoleOnly.ThreadPool"), "Host"));
        Assert.Equal("Host", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information, "Some.Library"), "Host"));
        Assert.Equal("Host", SerilogRouting.LoggerName(Events.Create(LogEventLevel.Information), "Host"));
    }
}
