using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace HsnSoft.Base.AspNetCore.Serilog;

public static class SerilogConfigurationHelper
{
    public static ILogger ConfigureConsoleLogger(IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
#if DEBUG
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
#else
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
#endif
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName);

        try
        {
            var loglevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), configuration["FrameworkLogger:LogLevel"] ?? throw new InvalidOperationException());
            loggerConfiguration = loggerConfiguration
                .WriteTo.Conditional(logEvent => (byte)logEvent.Level >= (byte)loglevel, sinkConfiguration =>
                {
                    sinkConfiguration.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                        theme: AnsiConsoleTheme.Sixteen
                    );
                });
        }
        catch (Exception)
        {
            // no config
            loggerConfiguration = loggerConfiguration
                .WriteTo.Async(c => c.Console // All logs , Verbose,Debug,Information, Warning, Error, Fatal
                (
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                    theme: AnsiConsoleTheme.Sixteen
                ));
        }

        return loggerConfiguration.CreateLogger();
    }

    internal static ILogger ConfigureFilePersistentLogger(IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
#if DEBUG
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
#else
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
#endif
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .WriteTo.Conditional(logEvent => logEvent is { Level: LogEventLevel.Verbose or LogEventLevel.Fatal },
                sinkConfiguration => sinkConfiguration.File("Logs/logs.txt")
            );

        try
        {
            var loglevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), configuration["FrameworkLogger:LogLevel"] ?? throw new InvalidOperationException());
            loggerConfiguration = loggerConfiguration
                .WriteTo.Conditional(logEvent => (byte)logEvent.Level >= (byte)loglevel, sinkConfiguration =>
                {
                    sinkConfiguration.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                        theme: AnsiConsoleTheme.Sixteen
                    );
                });
        }
        catch (Exception)
        {
            // no config
            loggerConfiguration = loggerConfiguration
                .WriteTo.Async(c => c.Console // All logs , Verbose,Debug,Information, Warning, Error, Fatal
                (
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                    theme: AnsiConsoleTheme.Sixteen
                ));
        }

        return loggerConfiguration.CreateLogger();
    }
}