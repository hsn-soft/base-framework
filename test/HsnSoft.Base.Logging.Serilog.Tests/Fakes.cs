using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace HsnSoft.Base.Logging.Serilog.Tests;

/// <summary>Serilog's SelfLog and the console are process-wide: tests that touch them do not run next to each other.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessWideCollection
{
    public const string Name = "process-wide";
}

public sealed class CaptureSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_events)
            {
                return [.. _events];
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_events)
        {
            _events.Add(logEvent);
        }
    }

    public IEnumerable<string> Messages => Events.Select(logEvent => logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture));
}

public sealed class DisposableSink : ILogEventSink, IDisposable
{
    public List<LogEvent> Events { get; } = [];

    public bool Disposed { get; private set; }

    public void Emit(LogEvent logEvent) => Events.Add(logEvent);

    public void Dispose() => Disposed = true;
}

internal static class Events
{
    internal static LogEvent Create(LogEventLevel level, string? sourceContext = null, string message = "message")
    {
        List<LogEventProperty> properties = [];
        if (sourceContext is not null)
        {
            properties.Add(new LogEventProperty(Constants.SourceContextPropertyName, new ScalarValue(sourceContext)));
        }

        return new LogEvent(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse(message), properties);
    }
}
