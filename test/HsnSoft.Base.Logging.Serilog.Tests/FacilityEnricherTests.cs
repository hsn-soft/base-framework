using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog.Tests;

public sealed class FacilityEnricherTests
{
    private sealed class Factory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }

    private static LogEvent Event(params LogEventProperty[] properties)
        => new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplate("x", []), properties);

    private static LogEventProperty Structure(string name, params (string Name, object? Value)[] members)
        => new(name, new StructureValue(members.Select(member => new LogEventProperty(member.Name, new ScalarValue(member.Value)))));

    private static string? Value(LogEvent logEvent, string name)
        => logEvent.Properties.TryGetValue(name, out LogEventPropertyValue? value) ? ((ScalarValue)value).Value as string : null;

    private static LogEvent Enriched(params LogEventProperty[] properties)
    {
        LogEvent logEvent = Event(properties);
        new FacilityEnricher().Enrich(logEvent, new Factory());
        return logEvent;
    }

    [Fact]
    public void TheNameOfALogsEvent_BecomesItsFacility()
    {
        LogEvent logEvent = Enriched(Structure("EventId", ("Id", 2000), ("Name", "CONSUME_EVENT_SUCCESS")));

        Assert.Equal("CONSUME_EVENT_SUCCESS", Value(logEvent, "Facility"));
    }

    [Fact]
    public void TheFacilityAndTheCorrelationIdOfAStructuredLog_BecomeTopLevelProperties()
    {
        LogEvent logEvent = Enriched(Structure("Log", ("Facility", "APPLICATION_LOADER_STARTED"), ("CorrelationId", "corr-1")));

        Assert.Equal("APPLICATION_LOADER_STARTED", Value(logEvent, "Facility"));
        Assert.Equal("corr-1", Value(logEvent, "CorrelationId"));
    }

    [Fact]
    public void TheFacilityOfAStructuredLog_WinsOverTheNameOfItsEvent()
    {
        LogEvent logEvent = Enriched(Structure("Log", ("Facility", "FROM_LOG")), Structure("EventId", ("Name", "FROM_EVENT")));

        Assert.Equal("FROM_LOG", Value(logEvent, "Facility"));
    }

    [Fact]
    public void AFacilityTheLogAlreadyHas_IsLeftAlone()
    {
        LogEvent logEvent = Enriched(new LogEventProperty("Facility", new ScalarValue("OWN")), new LogEventProperty("CorrelationId", new ScalarValue("own-corr")), Structure("Log", ("Facility", "OTHER"), ("CorrelationId", "other")));

        Assert.Equal("OWN", Value(logEvent, "Facility"));
        Assert.Equal("own-corr", Value(logEvent, "CorrelationId"));
    }

    [Theory]
    [InlineData("ExecutedEndpoint")]
    [InlineData("Consume_Event")]
    [InlineData("ÇÖZÜM")]
    public void TheNameOfAPlatformEvent_IsNotAFacility(string name)
    {
        LogEvent logEvent = Enriched(Structure("EventId", ("Id", 1), ("Name", name)));

        Assert.False(logEvent.Properties.ContainsKey("Facility"));
    }

    [Fact]
    public void LogsWithoutAFacility_GetNone()
    {
        LogEvent plain = Enriched();
        LogEvent unnamedEvent = Enriched(Structure("EventId", ("Id", 5), ("Name", null)));
        LogEvent emptyFacility = Enriched(Structure("Log", ("Facility", ""), ("CorrelationId", null)));
        LogEvent scalarLog = Enriched(new LogEventProperty("Log", new ScalarValue("text")));

        foreach (LogEvent logEvent in new[] { plain, unnamedEvent, emptyFacility, scalarLog })
        {
            Assert.False(logEvent.Properties.ContainsKey("Facility"));
            Assert.False(logEvent.Properties.ContainsKey("CorrelationId"));
        }
    }
}
