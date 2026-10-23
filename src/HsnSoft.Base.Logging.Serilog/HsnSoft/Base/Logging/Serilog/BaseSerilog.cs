using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
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
    // what the services in production always did: the libraries under the application talk less than the application
    private static readonly string[] s_dependencyCategories =
    [
        "System",
        "Microsoft",
        "Microsoft.Hosting",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "HsnSoft.Base.EntityFrameworkCore",
        "MongoDB",
        "HsnSoft.Base.MongoDB",
    ];

    internal static BaseSerilogSetup Create(FrameworkLoggerOptions options, string loggerName, Action<LoggerConfiguration>? configure, SerilogSinks? sinks = null)
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
        LogEventLevel console = SerilogRouting.ToSerilog(options.LogLevel);
        LogEventLevel persistent = SerilogRouting.ToSerilog(options.EffectivePersistentLevel);

        LoggerConfiguration configuration = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Solution", AppDomain.CurrentDomain.FriendlyName.Split('.')[0])
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .Enrich.With(new LoggerNameEnricher(loggerName));

        foreach (KeyValuePair<string, LogLevel> level in Overrides(options))
        {
            configuration = configuration.MinimumLevel.Override(level.Key, SerilogRouting.ToSerilog(level.Value));
        }

        configuration = configuration
            .WriteTo.Conditional(logEvent => SerilogRouting.ForConsole(logEvent, console) && SerilogRouting.ShowsProperties(logEvent), sink => sink.Sink(resolved.ConsoleWithProperties))
            .WriteTo.Conditional(logEvent => SerilogRouting.ForConsole(logEvent, console) && !SerilogRouting.ShowsProperties(logEvent), sink => sink.Sink(resolved.Console))
            .WriteTo.Conditional(logEvent => SerilogRouting.ForPersistent(logEvent, persistent), sink => sink.Sink(resolved.Persistent));

        configure?.Invoke(configuration);
        return new BaseSerilogSetup(configuration.CreateLogger(), resolved.Refreshable);
    }

    /// <summary>The defaults (libraries one step quieter than the application, Warning once the application is quiet itself) then what the configuration says.</summary>
    internal static IEnumerable<KeyValuePair<string, LogLevel>> Overrides(FrameworkLoggerOptions options)
    {
        LogLevel dependencies = options.LogLevel switch
        {
            LogLevel.Trace => LogLevel.Debug,
            LogLevel.Debug => LogLevel.Information,
            _ => LogLevel.Warning,
        };

        Dictionary<string, LogLevel> levels = [];
        foreach (string category in s_dependencyCategories)
        {
            levels[category] = dependencies;
        }

        foreach (KeyValuePair<string, LogLevel> level in options.LevelOverrides)
        {
            levels[level.Key] = level.Value;
        }

        return levels;
    }

    private sealed class LoggerNameEnricher(string hostName) : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            => logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LoggerName", SerilogRouting.LoggerName(logEvent, hostName)));
    }
}
