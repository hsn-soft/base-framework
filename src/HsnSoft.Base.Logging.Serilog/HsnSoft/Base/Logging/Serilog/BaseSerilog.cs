using System;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>The Serilog logger of a service and the part of it that is rebuilt on a schedule.</summary>
/// <param name="Logger">The logger; assign it to <c>Log.Logger</c> or hand it to a logging builder.</param>
/// <param name="Refreshable">The persistent sink when it can be rebuilt, <see langword="null"/> otherwise.</param>
internal sealed record BaseSerilogSetup(Logger Logger, RefreshableSink? Refreshable);

internal static class BaseSerilog
{
    internal static BaseSerilogSetup Create(PersistentLoggerOptions options, string loggerName, Action<LoggerConfiguration>? configure, SerilogSinks? sinks = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        if (options.IsSelfLogActive)
        {
            // Serilog swallows the errors of its sinks on purpose (an unreachable Graylog must not break the application), so without
            // this they leave no trace. They go to the error output, not to a Serilog sink: a failing sink must not log into itself.
            SelfLog.Enable(message => Console.Error.WriteLine($"[Serilog SelfLog] {message}"));
        }

        SerilogSinks resolved = sinks ?? SerilogSinks.Create(options);
        LogLevelRules console = options.ConsoleLevels;
        PersistentLevelRules persistent = new(options);

        string solution = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
        LoggerConfiguration configuration = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Solution", solution)
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .Enrich.With(new VersionEnricher(options.ResolveVersion()))
            .Enrich.With(new LoggerNameEnricher(loggerName))
            .Enrich.With<FacilityEnricher>()
            .Enrich.With(new LogShapeEnricher(string.IsNullOrWhiteSpace(options.AppNamespace) ? solution : options.AppNamespace.Trim()))
            .Enrich.With<NoiseEnricher>();

        configuration = configuration
            .WriteTo.Conditional(logEvent => SerilogRouting.ForConsole(logEvent, console), sink => sink.Sink(resolved.Console))
            .WriteTo.Conditional(logEvent => SerilogRouting.ForPersistent(logEvent, persistent), sink => sink.Sink(new FieldGuardSink(resolved.Persistent, options.MaxPersistentFieldLength)));

        configure?.Invoke(configuration);
        return new BaseSerilogSetup(configuration.CreateLogger(), resolved.Refreshable);
    }

    private sealed class VersionEnricher(string? version) : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (version is not null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Version", version));
            }
        }
    }

    private sealed class LoggerNameEnricher(string hostName) : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            => logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LoggerName", SerilogRouting.LoggerName(logEvent, hostName)));
    }
}
