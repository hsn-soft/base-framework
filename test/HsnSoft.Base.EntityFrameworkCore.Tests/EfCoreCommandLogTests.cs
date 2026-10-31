using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreCommandLogTests
{
    private const string Category = "Microsoft.EntityFrameworkCore.Database.Command";

    private sealed class CommandContext(DbContextOptions<CommandContext> options) : DbContext(options);

    private sealed class Sqlite : IEfCoreProvider
    {
        public string Name => "sqlite";

        public void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlite(connectionString);
    }

    private sealed class Memory : ILoggerProvider
    {
        public List<LogLevel> Levels { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose()
        {
        }

        private sealed class Logger(Memory owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => owner.Levels.Add(logLevel);
        }
    }

    private static List<LogLevel> Written(string? logCommands, string? configured = null)
    {
        Dictionary<string, string?> settings = new() { ["ConnectionStrings:EfCore-A"] = "Data Source=a.db" };
        if (logCommands is not null)
        {
            settings["EntityFramework:LogCommands"] = logCommands;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Memory memory = new();
        ServiceCollection services = new();
        services.AddLogging(logging =>
        {
            // what Logging:LogLevel:Microsoft.EntityFrameworkCore says, as the host reads it
            if (configured is not null)
            {
                logging.AddFilter("Microsoft.EntityFrameworkCore", Enum.Parse<LogLevel>(configured));
            }

            logging.AddProvider(memory);
        });
        services.AddBaseEfCore<CommandContext>(configuration, providers => providers.Add(new Sqlite()), "sqlite", "EfCore-A");
        using ServiceProvider provider = services.BuildServiceProvider();

        ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(Category);
        logger.Log(LogLevel.Information, new EventId(1), "executed", null, (text, _) => text);
        logger.Log(LogLevel.Error, new EventId(2), "failed", null, (text, _) => text);
        return memory.Levels;
    }

    [Fact]
    public void TheCommandsThatRun_AreNotLoggedByDefault_ButAFailedOneIs()
        => Assert.Equal([LogLevel.Error], Written(null));

    [Fact]
    public void TheSetting_TurnsTheLogOfTheCommandsOn()
        => Assert.Equal([LogLevel.Information, LogLevel.Error], Written("true"));

    [Fact]
    public void TheSetting_IsTheWord_WhateverTheLogLevelsSay()
    {
        Assert.Equal([LogLevel.Error], Written("false", "Information"));
        Assert.Equal([LogLevel.Information, LogLevel.Error], Written("true", "Warning"));
    }

    [Fact]
    public void TheOptions_DoNotLogCommandsByDefault() => Assert.False(new EfCoreOptions().LogCommands);
}
