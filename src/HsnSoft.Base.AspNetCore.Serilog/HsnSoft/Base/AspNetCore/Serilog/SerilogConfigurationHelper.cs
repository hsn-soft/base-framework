using System;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace HsnSoft.Base.AspNetCore.Serilog;

public static class SerilogConfigurationHelper
{
    public static ILogger ConfigureConsoleLogger()
    {
        ILogger logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .MinimumLevel.Verbose()
            .WriteTo.Async(c => c.Console // All logs , Verbose,Debug,Information, Warning, Error, Fatal
            (
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                theme: AnsiConsoleTheme.Sixteen
            ))
            .CreateLogger();

        return logger;
    }

    internal static ILogger ConfigureFilePersistentLogger()
    {
        ILogger logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Assembly", AppDomain.CurrentDomain.FriendlyName)
            .MinimumLevel.Verbose()
            .WriteTo.Conditional(logEvent => logEvent is { Level: LogEventLevel.Verbose or LogEventLevel.Fatal },
                sinkConfiguration => sinkConfiguration.File("Logs/logs.txt")
            )
            .WriteTo.Async(c => c.Console // All logs , Verbose,Debug,Information, Warning, Error, Fatal
            (
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}{NewLine}",
                theme: AnsiConsoleTheme.Sixteen
            ))
            .CreateLogger();

        return logger;
    }
}

