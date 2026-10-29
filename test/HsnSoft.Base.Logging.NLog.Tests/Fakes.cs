using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace HsnSoft.Base.Logging.NLog.Tests;

/// <summary>NLog's internal logger and the console are process-wide: tests that touch them do not run next to each other.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessWideCollection
{
    public const string Name = "process-wide";
}

/// <summary>A target that keeps what it is given, and the line its layout makes of it.</summary>
public sealed class CaptureTarget : TargetWithLayout
{
    private readonly List<LogEventInfo> _events = [];
    private readonly List<string> _lines = [];

    public CaptureTarget() => Name = $"capture-{Guid.NewGuid():N}";

    public IReadOnlyList<LogEventInfo> Events
    {
        get
        {
            lock (_events)
            {
                return [.. _events];
            }
        }
    }

    public IReadOnlyList<string> Lines
    {
        get
        {
            lock (_events)
            {
                return [.. _lines];
            }
        }
    }

    protected override void Write(LogEventInfo logEvent)
    {
        string line = Layout is null ? string.Empty : RenderLogEvent(Layout, logEvent);
        lock (_events)
        {
            _events.Add(logEvent);
            _lines.Add(line);
        }
    }
}

/// <summary>A service's logging with the two places captured: what the console would show and what would be persistent.</summary>
public sealed class Harness : IDisposable
{
    private readonly ILoggerFactory _factory;

    public Harness(Action<PersistentLoggerOptions>? configure = null, string loggerName = "Host", Action<global::NLog.Config.LoggingConfiguration>? configureNLog = null)
    {
        // what is not about the level of the console sees everything in it
        PersistentLoggerOptions options = new() { ConsoleLevels = new LogLevelRules(Microsoft.Extensions.Logging.LogLevel.Trace), AppNamespace = "Acme" };
        configure?.Invoke(options);
        BaseNLogSetup setup = BaseNLog.Create(options, loggerName, configureNLog, Console, Persistent);
        Shape = setup.Shape;
        _factory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
            .AddProvider(new NLogLoggerProvider(new NLogProviderOptions { IncludeScopes = true, CaptureMessageTemplates = true, CaptureMessageProperties = true, ParseMessageTemplates = true }, setup.Factory)));
    }

    public CaptureTarget Console { get; } = new();

    public CaptureTarget Persistent { get; } = new();

    internal LogShapeTarget Shape { get; }

    public Microsoft.Extensions.Logging.ILogger Logger(string category) => _factory.CreateLogger(category);

    public void Dispose() => _factory.Dispose();
}
