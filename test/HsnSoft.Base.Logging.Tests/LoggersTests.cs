using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class LoggersTests
{
    [Fact]
    public void ConsoleLogger_WritesUnderAConsoleOnlyCategory_AtTheRightLevels()
    {
        ListLoggerFactory factory = new();
        AppConsoleLogger logger = new(factory, "ThreadPool");

        logger.LogDebug("debug {Value}", 1);
        logger.LogInformation("information {Value}", 2);
        logger.LogWarning("warning {Value}", 3);
        logger.LogError("error {Value}", 4);
        logger.LogError(new InvalidOperationException("boom"), "failed {Value}", 5);

        Assert.Equal("ConsoleOnly.ThreadPool", Assert.Single(factory.Categories));
        Assert.Equal(
            [LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Error],
            factory.Entries.Select(entry => entry.Level));
        Assert.Equal(["debug 1", "information 2", "warning 3", "error 4", "failed 5"], factory.Entries.Select(entry => entry.Message));
        Assert.All(factory.Entries, entry => Assert.StartsWith(LogCategories.ConsoleOnlyPrefix, entry.Category, StringComparison.Ordinal));
        Assert.Equal("boom", factory.Entries[^1].Exception?.Message);
    }

    [Fact]
    public void ConsoleLogger_HasADefaultName_AndNeedsAFactoryAndAName()
    {
        ListLoggerFactory factory = new();

        _ = new AppConsoleLogger(factory);

        Assert.Equal("ConsoleOnly.AppLogger", Assert.Single(factory.Categories));
        Assert.Throws<ArgumentNullException>(() => new AppConsoleLogger(null!));
        Assert.Throws<ArgumentException>(() => new AppConsoleLogger(factory, " "));
    }

    [Fact]
    public void BaseLogging_RegistersTheLoggersAndTheMasker()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(new ListLoggerFactory());
        services.AddBaseLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<AppConsoleLogger>(provider.GetRequiredService<IAppConsoleLogger>());
        Assert.IsType<LogMasker>(provider.GetRequiredService<ILogMasker>());
        Assert.Same(provider.GetRequiredService<IAppConsoleLogger>(), provider.GetRequiredService<IAppConsoleLogger>());
    }

    [Fact]
    public void BaseLogging_KeepsWhatTheProductRegisteredItself()
    {
        ServiceCollection services = new();
        ListLoggerFactory factory = new();
        AppConsoleLogger own = new(factory, "Own");
        services.AddSingleton<ILoggerFactory>(factory);
        services.AddSingleton<IAppConsoleLogger>(own);
        services.AddBaseLogging();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(own, provider.GetRequiredService<IAppConsoleLogger>());
    }

    [Fact]
    public void NullServices_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => LoggingServiceCollectionExtensions.AddBaseLogging(null!));
    }
}
