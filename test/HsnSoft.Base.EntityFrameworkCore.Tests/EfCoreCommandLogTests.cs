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
        public List<(string Category, LogLevel Level)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class Logger(Memory owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => owner.Entries.Add((category, logLevel));
        }
    }

    // the commands that the context runs, as the log of a host with a provider that has a rule of its own sees them (the Serilog provider of the framework has one that opens its level wide, which a rule of a category does not beat)
    private static async Task<List<(string Category, LogLevel Level)>> Written(string? logCommands, bool failing = false)
    {
        string file = Path.Combine(Path.GetTempPath(), $"efcommandlog-{Guid.NewGuid():N}.db");
        Dictionary<string, string?> settings = new() { ["ConnectionStrings:EfCore-A"] = $"Data Source={file}" };
        if (logCommands is not null)
        {
            settings["EntityFramework:LogCommands"] = logCommands;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Memory memory = new();
        ServiceCollection services = new();
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(memory);
            logging.AddFilter<Memory>(null, LogLevel.Trace);
        });
        services.AddBaseEfCore<CommandContext>(configuration, providers => providers.Add(new Sqlite()), "sqlite", "EfCore-A");
        await using ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            using IServiceScope scope = provider.CreateScope();
            CommandContext context = scope.ServiceProvider.GetRequiredService<CommandContext>();
            if (failing)
            {
                await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlRawAsync("select * from nowhere"));
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync("create table t (id integer)");
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(file);
        }

        return memory.Entries;
    }

    [Fact]
    public async Task TheCommandsThatRun_AreNotLoggedByDefault()
        => Assert.DoesNotContain(await Written(null), entry => entry.Category == Category && entry.Level == LogLevel.Information);

    [Fact]
    public async Task TheSetting_TurnsTheLogOfTheCommandsOn()
        => Assert.Contains(await Written("true"), entry => entry.Category == Category && entry.Level == LogLevel.Information);

    [Fact]
    public async Task TheSettingFalse_IsTheSameAsNone()
        => Assert.DoesNotContain(await Written("false"), entry => entry.Category == Category && entry.Level == LogLevel.Information);

    [Fact]
    public async Task ACommandThatFails_IsLoggedWhateverTheSettingSays()
        => Assert.Contains(await Written(null, failing: true), entry => entry.Category == Category && entry.Level == LogLevel.Error);

    [Fact]
    public void TheOptions_DoNotLogCommandsByDefault() => Assert.False(new EfCoreOptions().LogCommands);
}
