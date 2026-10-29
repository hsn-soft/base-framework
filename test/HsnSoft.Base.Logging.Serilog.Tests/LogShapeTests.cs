using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace HsnSoft.Base.Logging.Serilog.Tests;

public sealed class LogShapeTests
{
    private sealed class Factory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }

    private sealed class CollectSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static LogEvent Event(params LogEventProperty[] properties)
        => new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplate("x", []), properties);

    private static LogEventProperty Source(string category) => new("SourceContext", new ScalarValue(category));

    private static LogEventProperty Structure(string name) => new(name, new StructureValue([new LogEventProperty("A", new ScalarValue(1))]));

    private static string? Value(LogEvent logEvent, string name) => ((ScalarValue)logEvent.Properties[name]).Value as string;

    private static LogEvent Enriched(params LogEventProperty[] properties)
    {
        LogEvent logEvent = Event(properties);
        new LogShapeEnricher("Orders").Enrich(logEvent, new Factory());
        return logEvent;
    }

    [Theory]
    [InlineData("HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer", "FrameworkLog")]
    [InlineData("HsnSoft.Base", "FrameworkLog")]
    [InlineData("Orders.Checkout.OrderService", "AppLog")]
    [InlineData("Orders", "AppLog")]
    [InlineData("Orders.Shared.RateLimiting.RateLimitMiddleware", "SharedLog")]
    [InlineData("Orders.Shared", "SharedLog")]
    [InlineData("Orders.SharedExtras.Thing", "AppLog")]
    [InlineData("Orders.Checkout.Shared.Thing", "AppLog")]
    [InlineData("ConsoleOnly.ThreadPool", "AppLog")]
    [InlineData("", "AppLog")]
    // everything else is the platform's: no list of libraries
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware", "PlatformLog")]
    [InlineData("System.Net.Http.HttpClient", "PlatformLog")]
    [InlineData("Npgsql.Command", "PlatformLog")]
    [InlineData("Polly.Retry", "PlatformLog")]
    [InlineData("OrdersExtras.Thing", "PlatformLog")]
    [InlineData("HsnSoft.BaseCamp.Thing", "PlatformLog")]
    [InlineData("Acmed.Other", "PlatformLog")]
    public void TheCategory_DecidesTheLogType(string category, string expected)
    {
        LogEvent logEvent = category.Length == 0 ? Enriched() : Enriched(Source(category));

        Assert.Equal(expected, Value(logEvent, "LogType"));
    }

    [Fact]
    public void ALogThatSaysItsType_KeepsIt()
    {
        LogEvent logEvent = Enriched(Source("HsnSoft.Base.EventBus.EventDispatcher"), new LogEventProperty("LogType", new ScalarValue("ConsumeStart")));

        Assert.Equal("ConsumeStart", Value(logEvent, "LogType"));
    }

    [Fact]
    public void AnObjectMakesTheMessageStructured_PlainValuesMakeItArgs()
    {
        Assert.Equal("Structured", Value(Enriched(Structure("Order")), "MessageType"));
        Assert.Equal("Structured", Value(Enriched(new LogEventProperty("Map", new DictionaryValue([]))), "MessageType"));
        Assert.Equal("Args", Value(Enriched(new LogEventProperty("Count", new ScalarValue(3))), "MessageType"));
        Assert.Equal("Args", Value(Enriched(Structure("EventId")), "MessageType"));
        Assert.Equal("Args", Value(Enriched(), "MessageType"));
    }

    [Fact]
    public void FieldGuard_TurnsObjectsIntoOneJsonFieldAndCutsLongText()
    {
        CollectSink inner = new();
        FieldGuardSink guard = new(inner, maxLength: 20);
        LogEvent logEvent = Event(
            Structure("Body"),
            new LogEventProperty("Items", new SequenceValue([new ScalarValue(1), new ScalarValue(2)])),
            new LogEventProperty("Map", new DictionaryValue([new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("k"), new ScalarValue("v"))])),
            new LogEventProperty("Text", new ScalarValue(new string('x', 50))),
            new LogEventProperty("Short", new ScalarValue("ok")),
            new LogEventProperty("Count", new ScalarValue(7)),
            Structure("EventId"));

        guard.Emit(logEvent);

        LogEvent written = Assert.Single(inner.Events);
        Assert.Equal("{\"A\":1}", Value(written, "Body"));
        Assert.Equal("[1,2]", Value(written, "Items"));
        Assert.Equal("{\"k\":\"v\"}", Value(written, "Map"));
        Assert.Equal("xxxxxxxxxxxxxxxxxxxx...(+30)", Value(written, "Text"));
        Assert.Equal("ok", Value(written, "Short"));
        Assert.Equal(7, ((ScalarValue)written.Properties["Count"]).Value);
        Assert.False(written.Properties.ContainsKey("EventId"));
    }

    [Fact]
    public void TheConnectionId_IsNotAFieldOfEveryLog()
    {
        LogEvent logEvent = Event(new LogEventProperty("ConnectionId", new ScalarValue("0HN")), new LogEventProperty("RequestId", new ScalarValue("0HN:1")));

        new NoiseEnricher().Enrich(logEvent, new Factory());

        Assert.False(logEvent.Properties.ContainsKey("ConnectionId"));
        Assert.True(logEvent.Properties.ContainsKey("RequestId"));
    }

    [Theory]
    [InlineData("FrameworkLog", false)]
    [InlineData("PlatformLog", false)]
    [InlineData("AppLog", true)]
    [InlineData("SharedLog", true)]
    [InlineData("HttpRequest", true)]
    [InlineData("HttpResponse", true)]
    [InlineData("HttpRequestResponse", true)]
    [InlineData("ConsumeStart", true)]
    [InlineData("ConsumeEnd", true)]
    public void WhoAsked_IsOnlyOnTheLogsOfTheRequestTheMessageAndTheApplication(string logType, bool kept)
    {
        LogEvent logEvent = Event(
            new LogEventProperty("LogType", new ScalarValue(logType)),
            new LogEventProperty("UserId", new ScalarValue("u1")),
            new LogEventProperty("UserRoles", new ScalarValue("admin")),
            new LogEventProperty("ClientChannel", new ScalarValue("ios")),
            new LogEventProperty("ClientVersion", new ScalarValue("1.2")),
            new LogEventProperty("ClientLat", new ScalarValue("1")),
            new LogEventProperty("ClientLong", new ScalarValue("2")),
            new LogEventProperty("CorrelationId", new ScalarValue("c")));

        new NoiseEnricher().Enrich(logEvent, new Factory());

        Assert.Equal(kept, logEvent.Properties.ContainsKey("UserId"));
        Assert.Equal(kept, logEvent.Properties.ContainsKey("ClientLat"));
        Assert.True(logEvent.Properties.ContainsKey("CorrelationId"));
    }

    [Theory]
    [InlineData("AppLog", false)]
    [InlineData("SharedLog", false)]
    [InlineData("HttpRequest", true)]
    [InlineData("HttpResponse", true)]
    [InlineData("PlatformLog", true)]
    [InlineData("FrameworkLog", true)]
    public void TheEndpointTheSpanAndTheRequestOfALog_AreNotOnAnAppLog(string logType, bool kept)
    {
        string[] fields = ["ActionId", "ActionName", "ParentId", "RequestId", "RequestPath", "SpanId"];
        LogEvent logEvent = Event([new LogEventProperty("LogType", new ScalarValue(logType)), new LogEventProperty("OrderNo", new ScalarValue("A-1")), new LogEventProperty("TraceId", new ScalarValue("t")), .. fields.Select(field => new LogEventProperty(field, new ScalarValue("x")))]);

        new NoiseEnricher().Enrich(logEvent, new Factory());

        Assert.All(fields, field => Assert.Equal(kept, logEvent.Properties.ContainsKey(field)));
        // what the developer wrote and the chain's ids stay
        Assert.True(logEvent.Properties.ContainsKey("OrderNo"));
        Assert.True(logEvent.Properties.ContainsKey("TraceId"));
    }

    [Fact]
    public void ALogWithoutAType_IsLeftAlone()
    {
        LogEvent logEvent = Event(new LogEventProperty("SpanId", new ScalarValue("x")));

        new NoiseEnricher().Enrich(logEvent, new Factory());

        Assert.True(logEvent.Properties.ContainsKey("SpanId"));
    }

    [Fact]
    public void TheDetailsOfAMessage_AreInTheMessageAndNotAField()
    {
        CollectSink inner = new();
        FieldGuardSink guard = new(inner, maxLength: 100);
        LogEvent logEvent = new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse("done{MessageDetails}"),
            [new LogEventProperty("MessageDetails", new ScalarValue(" (2 files)")), new LogEventProperty("Kept", new ScalarValue(1))]);

        guard.Emit(logEvent);

        LogEvent written = Assert.Single(inner.Events);
        Assert.Equal("done (2 files)", written.MessageTemplate.Text);
        Assert.False(written.Properties.ContainsKey("MessageDetails"));
        Assert.True(written.Properties.ContainsKey("Kept"));
    }

    [Fact]
    public void FieldGuard_WritesTheMessageAsPeopleReadIt()
    {
        CollectSink inner = new();
        FieldGuardSink guard = new(inner, maxLength: 100);
        MessageTemplate template = new MessageTemplateParser().Parse("{From} -> {To}: {Count} times {Order} {Missing}.");
        LogEvent logEvent = new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            template,
            [
                new LogEventProperty("From", new ScalarValue("httpclient")),
                new LogEventProperty("To", new ScalarValue("token-login")),
                new LogEventProperty("Count", new ScalarValue(3)),
                new LogEventProperty("Order", new StructureValue([new LogEventProperty("A", new ScalarValue(1))])),
            ]);

        guard.Emit(logEvent);

        Assert.Equal("httpclient -> token-login: 3 times { A: 1 } {Missing}.", Assert.Single(inner.Events).MessageTemplate.Text);
    }

    [Fact]
    public void FieldGuard_CutsALongJsonObject()
    {
        CollectSink inner = new();
        FieldGuardSink guard = new(inner, maxLength: 5);

        guard.Emit(Event(Structure("Body")));

        Assert.Equal("{\"A\":...(+2)", Value(Assert.Single(inner.Events), "Body"));
    }
}
