using System;
using System.Linq;
using Destructurama;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace HsnSoft.Base.AspNetCore.Serilog;

public static class SerilogConfigurationHelper
{
    public static ILogger ConfigureConsoleLogger(IConfiguration configuration)
    {
        LogEventLevel loglevel;
        try
        {
            loglevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), configuration["FrameworkLogger:LogLevel"] ?? throw new InvalidOperationException());
        }
        catch (Exception)
        {
            loglevel = LogEventLevel.Verbose;
        }

        var dependencyAssemblyLogLevel = loglevel switch
        {
            LogEventLevel.Verbose => LogEventLevel.Debug,
            LogEventLevel.Debug => LogEventLevel.Information,
            _ => LogEventLevel.Warning
        };

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("System", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", dependencyAssemblyLogLevel)
            .Destructure.JsonNetTypes()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Solution", AppDomain.CurrentDomain.FriendlyName.Split('.').First())
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName);


        return loggerConfiguration.WriteTo.Conditional(logEvent => (byte)logEvent.Level >= (byte)loglevel, sinkConfiguration =>
        {
            sinkConfiguration.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}",
                theme: AnsiConsoleTheme.Sixteen
            );
        }).CreateLogger();
    }

    internal static ILogger ConfigureFilePersistentLogger(IConfiguration configuration)
    {
        LogEventLevel loglevel;
        try
        {
            loglevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), configuration["FrameworkLogger:LogLevel"] ?? throw new InvalidOperationException());
            Console.WriteLine($"=== FRAMEWORK LOG LEVEL : {loglevel.ToString()} ===");
        }
        catch (Exception)
        {
            loglevel = LogEventLevel.Verbose;
            Console.WriteLine($"=== FRAMEWORK LOG LEVEL : UNKNOWN ===");
        }

        var dependencyAssemblyLogLevel = loglevel switch
        {
            LogEventLevel.Verbose => LogEventLevel.Debug,
            LogEventLevel.Debug => LogEventLevel.Information,
            _ => LogEventLevel.Warning
        };

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("System", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", dependencyAssemblyLogLevel)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", dependencyAssemblyLogLevel)
            .Destructure.JsonNetTypes()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Solution", AppDomain.CurrentDomain.FriendlyName.Split('.').First())
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName);

        var isGrayLogActive = false;
        try
        {
            if (bool.Parse(configuration["FrameworkLogger:IsGrayLogActive"] ?? throw new InvalidOperationException()))
            {
                isGrayLogActive = true;
                int.TryParse(configuration["FrameworkLogger:GrayLog:Port"], out var grayLogPort);
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Conditional(logEvent => logEvent is { Level: LogEventLevel.Verbose or LogEventLevel.Fatal }, sinkConfiguration =>
                    {
                        sinkConfiguration.Graylog(
                            new GraylogSinkOptions
                            {
                                HostnameOrAddress = configuration["FrameworkLogger:GrayLog:Address"],
                                TransportType = TransportType.Http,
                                Port = grayLogPort
                            });
                    });

                Console.WriteLine("=== SERILOG GRAYLOG SINK ACTIVE ===");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("=== SERILOG GRAYLOG SINK ERROR ===");
        }

        if (!isGrayLogActive)
        {
            loggerConfiguration = loggerConfiguration
                .WriteTo.Conditional(logEvent => logEvent is { Level: LogEventLevel.Verbose or LogEventLevel.Fatal },
                    sinkConfiguration => sinkConfiguration.File("Logs/logs.txt")
                );

            Console.WriteLine("=== SERILOG FILE SINK ACTIVE ===");
        }

        return loggerConfiguration.WriteTo.Conditional(logEvent => (byte)logEvent.Level >= (byte)loglevel, sinkConfiguration =>
        {
            sinkConfiguration.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}",
                theme: AnsiConsoleTheme.Sixteen
            );
        }).CreateLogger();
    }
}