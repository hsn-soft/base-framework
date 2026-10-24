using System;
using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using Serilog.Sinks.SystemConsole.Themes;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>The places a log can go to. A test builds its own set; the application gets <see cref="Create"/>.</summary>
/// <param name="Console">The console, for everything.</param>
/// <param name="Persistent">Graylog, or a file when Graylog is off.</param>
/// <param name="Refreshable">The persistent sink when it can be rebuilt (Graylog), so it can be refreshed on a schedule.</param>
internal sealed record SerilogSinks(ILogEventSink Console, ILogEventSink Persistent, RefreshableSink? Refreshable)
{
    private const string PlainTemplate = "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {LoggerName} | {Message:lj}{NewLine}{Exception}{NewLine}";
    private const string FileTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {LoggerName} [{SourceContext}] | {Message:lj}{NewLine}{Exception}";

    internal static SerilogSinks Create(PersistentLoggerOptions options)
    {
        ILogEventSink plain = ConsoleSink(PlainTemplate);

        if (options.IsGrayLogActive)
        {
            RefreshableSink graylog = new(() => GraylogSink(options));
            return new SerilogSinks(plain, graylog, graylog);
        }

        return new SerilogSinks(plain, FileSink(options), null);
    }

    private static Logger ConsoleSink(string template)
        => new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(outputTemplate: template, formatProvider: CultureInfo.InvariantCulture, theme: AnsiConsoleTheme.Sixteen)
            .CreateLogger();

    private static Logger FileSink(PersistentLoggerOptions options)
        => new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(
                path: options.FilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: FileTemplate)
            .CreateLogger();

    private static Logger GraylogSink(PersistentLoggerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.GrayLog.Address))
        {
            throw new InvalidOperationException($"{PersistentLoggerOptions.SectionName}:GrayLog:Address is required when {PersistentLoggerOptions.SectionName}:IsGrayLogActive is true.");
        }

        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Graylog(new GraylogSinkOptions
            {
                HostnameOrAddress = options.GrayLog.Address,
                Port = options.GrayLog.Port,
                TransportType = TransportType.Http,
            })
            .CreateLogger();
    }
}
