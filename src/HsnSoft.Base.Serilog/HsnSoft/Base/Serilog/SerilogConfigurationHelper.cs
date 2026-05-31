using System;
using System.Linq;
using Destructurama;
using HsnSoft.Base.Serilog.Mask;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace HsnSoft.Base.Serilog;

public static class SerilogConfigurationHelper
{
    private static string GetConsoleTemplate(bool includeSourceContext = false, bool includeProperties = false, bool includeYearInTimestamp = false, bool extraEmptyLine = false)
    {
        string template = "[";

        if (includeYearInTimestamp)
        {
            template += "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} ";
        }
        else
        {
            template += "{Timestamp:HH:mm:ss.fff zzz} ";
        }

        template += "{Level:u3}] {LoggerName} ";

        if (includeSourceContext)
        {
            template += "[{SourceContext}] ";
        }

        template += "| {Message:lj}";

        if (includeProperties)
        {
            template += " {Properties:j}";
        }

        template += "{NewLine}{Exception}";

        if (extraEmptyLine)
        {
            template += "{NewLine}";
        }

        return template;
    }


    public static ILogger ConfigureConsoleWithPersistentLogger(IConfiguration configuration, string loggerName)
        => BaseConfigureLogger(configuration, loggerName, isEnabledPersistent: true);

    public static ILogger ConfigureConsoleLogger(IConfiguration configuration, string loggerName)
        => BaseConfigureLogger(configuration, loggerName, isEnabledPersistent: false);

    private static ILogger BaseConfigureLogger(IConfiguration configuration, string loggerName, bool isEnabledPersistent)
    {
        LogEventLevel configuredLevel = GetFrameworkLogLevel(configuration);
        var dependencyAssemblyLogLevel = GetDependencyAssemblyLogLevel(configuredLevel);

        var loggerConfiguration = new LoggerConfiguration()
            .Destructure.JsonNetTypes()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("System", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.Hosting", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("HsnSoft.Base.EntityFrameworkCore", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("MongoDB", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("HsnSoft.Base.MongoDB", dependencyAssemblyLogLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Solution", AppDomain.CurrentDomain.FriendlyName.Split('.').FirstOrDefault() ?? "Unknown")
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .Enrich.WithProperty("LoggerName", loggerName);

        if (isEnabledPersistent)
        {
            loggerConfiguration = ConfigurePersistentSink(loggerName, loggerConfiguration, configuration);
        }

        loggerConfiguration = ConfigureConsoleSink(loggerConfiguration, configuredLevel);

        return loggerConfiguration.CreateLogger();
    }

    private static LoggerConfiguration ConfigureConsoleSink(
        LoggerConfiguration loggerConfiguration,
        LogEventLevel configuredLevel)
    {
        // Custom logger console
        loggerConfiguration = loggerConfiguration.WriteTo.Conditional(
            logEvent => (byte)logEvent.Level >= (byte)configuredLevel && UsePropertyConsoleTemplate(logEvent),
            sinkConfiguration =>
            {
                sinkConfiguration.Console(
                    outputTemplate: GetConsoleTemplate(
                        includeYearInTimestamp: false,
                        includeSourceContext: false,
                        includeProperties: true,
                        extraEmptyLine: true),
                    theme: AnsiConsoleTheme.Sixteen
                );
            });

        // Framework / host console
        loggerConfiguration = loggerConfiguration.WriteTo.Conditional(
            logEvent => (byte)logEvent.Level >= (byte)configuredLevel && !UsePropertyConsoleTemplate(logEvent),
            sinkConfiguration =>
            {
                sinkConfiguration.Console(
                    outputTemplate: GetConsoleTemplate(
                        includeYearInTimestamp: false,
                        includeSourceContext: false,
                        includeProperties: false,
                        extraEmptyLine: true),
                    theme: AnsiConsoleTheme.Sixteen
                );
            });

        return loggerConfiguration;
    }

    private static LoggerConfiguration ConfigurePersistentSink(
        string loggerName,
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        bool isGrayLogActive = GetGraylogIsActive(configuration);

        if (isGrayLogActive)
        {
            try
            {
                string? address = configuration["FrameworkLogger:GrayLog:Address"];
                string? portText = configuration["FrameworkLogger:GrayLog:Port"];

                if (string.IsNullOrWhiteSpace(address))
                    throw new InvalidOperationException("FrameworkLogger:GrayLog:Address missing.");

                if (!int.TryParse(portText, out int grayLogPort))
                    throw new InvalidOperationException("FrameworkLogger:GrayLog:Port invalid.");

                // SADECE persistent logger'lar Graylog'a gitsin
                loggerConfiguration = loggerConfiguration.WriteTo.Conditional(
                    IsPersistentLogger,
                    sinkConfiguration =>
                    {
                        sinkConfiguration.Graylog(
                            new GraylogSinkOptions { HostnameOrAddress = address, Port = grayLogPort, TransportType = TransportType.Http });
                    });

                Console.WriteLine("");
                Console.WriteLine($"=== SERILOG GRAYLOG SINK ACTIVE (CUSTOM LOGGERS ONLY) | [ {loggerName} ] ===");
                Console.WriteLine("");
                return loggerConfiguration;
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine($"=== SERILOG GRAYLOG SINK ERROR | [ {loggerName} ] === {ex.Message}");
                Console.WriteLine("");
            }
        }

        string filePath = configuration["FrameworkLogger:FilePath"] ?? "Logs/logs.txt";

        // Custom logger file
        loggerConfiguration = loggerConfiguration.WriteTo.Conditional(
            UsePropertyConsoleTemplate,
            sinkConfiguration =>
            {
                sinkConfiguration.File(
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: GetConsoleTemplate(
                        includeYearInTimestamp: true,
                        includeSourceContext: true,
                        includeProperties: true,
                        extraEmptyLine: false));
            });

        // Framework / host file
        loggerConfiguration = loggerConfiguration.WriteTo.Conditional(
            logEvent => !UsePropertyConsoleTemplate(logEvent),
            sinkConfiguration =>
            {
                sinkConfiguration.File(
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: GetConsoleTemplate(
                        includeYearInTimestamp: true,
                        includeSourceContext: true,
                        includeProperties: false,
                        extraEmptyLine: false));
            });

        Console.WriteLine("");
        Console.WriteLine($"=== SERILOG FILE SINK ACTIVE | [ {loggerName} ] ===");
        Console.WriteLine("");

        return loggerConfiguration;
    }

    private static bool UsePropertyConsoleTemplate(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("UsePropertyConsole", out var value))
            return false;

        return value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPersistentLogger(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("IsPersistentLogger", out var value))
            return false;

        return value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static LogEventLevel GetFrameworkLogLevel(IConfiguration configuration)
    {
        try
        {
            string? text = configuration["FrameworkLogger:LogLevel"];
            if (string.IsNullOrWhiteSpace(text))
                return LogEventLevel.Verbose;

            return Enum.Parse<LogEventLevel>(text, true);
        }
        catch
        {
            return LogEventLevel.Verbose;
        }
    }

    private static bool GetGraylogIsActive(IConfiguration configuration)
    {
        try
        {
            return bool.Parse(configuration["FrameworkLogger:IsGrayLogActive"] ?? "false");
        }
        catch
        {
            return false;
        }
    }

    private static LogEventLevel GetDependencyAssemblyLogLevel(LogEventLevel logLevel)
    {
        return logLevel switch
        {
            LogEventLevel.Verbose => LogEventLevel.Debug,
            LogEventLevel.Debug => LogEventLevel.Information,
            _ => LogEventLevel.Warning
        };
    }
}