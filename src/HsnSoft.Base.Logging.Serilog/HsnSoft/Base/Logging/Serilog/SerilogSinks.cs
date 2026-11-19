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

        // the file is written when Graylog is off and IsFileLogActive is on (by default in the Development environment only: a container has a read only file system,
        // and a file in it is lost with it); otherwise the console is the only place
        return new SerilogSinks(plain, options.WritesFile ? FileSink(options) : NoSink(), null);
    }

    private static Logger NoSink() => new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger();

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
