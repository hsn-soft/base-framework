using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class PersistentLoggerOptionsTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => $"PersistentLogger:{x.Key}", x => (string?)x.Value)).Build();

    [Fact]
    public void Defaults_AreSafe()
    {
        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(Configuration());

        Assert.Equal(LogLevel.Information, options.LogLevel);
        Assert.Equal(LogLevel.Information, options.ConsoleLevels.Default);
        Assert.Equal(LogLevel.Warning, options.PlatformMinimumLevel);
        Assert.Null(options.AppNamespace);
        Assert.False(options.IsGrayLogActive);
        Assert.False(options.IsSelfLogActive);
        Assert.Equal("Logs/logs.txt", options.FilePath);
        Assert.Equal(16384, options.MaxPersistentFieldLength);
        Assert.Null(options.GrayLog.Address);
        Assert.Equal(12201, options.GrayLog.Port);
        Assert.Equal(5, options.GrayLog.SinkRefreshIntervalMinutes);
        Assert.Empty(options.LevelOverrides);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("many")]
    public void AFieldLengthThatMakesNoSense_KeepsTheDefault(string text)
        => Assert.Equal(16384, PersistentLoggerOptions.FromConfiguration(Configuration(("MaxPersistentFieldLength", text))).MaxPersistentFieldLength);

    [Fact]
    public void TheVersion_IsTheSetting_ThenTheEnvironmentVariable_ThenTheVersionOfTheAssembly()
    {
        Assembly assembly = typeof(PersistentLoggerOptionsTests).Assembly;
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        static string? Environment(string name) => name == "BUILD_NUMBER" ? "2610241530-sha-ab12cd3" : name == "MY_VERSION" ? "7" : null;

        Assert.Equal("1.2.3", new PersistentLoggerOptions { Version = "1.2.3" }.ResolveVersion(Environment, assembly));
        Assert.Equal("2610241530-sha-ab12cd3", new PersistentLoggerOptions().ResolveVersion(Environment, assembly));
        Assert.Equal("7", new PersistentLoggerOptions { VersionVariable = "MY_VERSION" }.ResolveVersion(Environment, assembly));
        Assert.Equal(informational, new PersistentLoggerOptions().ResolveVersion(_ => null, assembly));
        Assert.Equal(informational, new PersistentLoggerOptions().ResolveVersion(_ => " ", assembly));
        Assert.NotNull(new PersistentLoggerOptions().ResolveVersion());
    }

    [Fact]
    public void TheVersionSettings_AreRead()
    {
        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(Configuration(("Version", "9.9"), ("VersionVariable", "IMAGE_TAG")));
        PersistentLoggerOptions defaults = PersistentLoggerOptions.FromConfiguration(Configuration());

        Assert.Equal("9.9", options.Version);
        Assert.Equal("IMAGE_TAG", options.VersionVariable);
        Assert.Null(defaults.Version);
        Assert.Equal("BUILD_NUMBER", defaults.VersionVariable);
    }

    [Fact]
    public void Values_AreRead()
    {
        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(Configuration(
            ("LogLevel", "Error"),
            ("AppNamespace", "Hhs"),
            ("IsGrayLogActive", "true"),
            ("IsSelfLogActive", "True"),
            ("FilePath", "logs/app.txt"),
            ("MaxPersistentFieldLength", "1000"),
            ("GrayLog:Address", "http://graylog"),
            ("GrayLog:Port", "12345"),
            ("GrayLog:SinkRefreshIntervalMinutes", "2"),
            ("LevelOverrides:Microsoft.AspNetCore", "Error"),
            ("LevelOverrides:MyApp", "Debug")));

        Assert.Equal(LogLevel.Error, options.LogLevel);
        Assert.Equal("Hhs", options.AppNamespace);
        Assert.True(options.IsGrayLogActive);
        Assert.True(options.IsSelfLogActive);
        Assert.Equal("logs/app.txt", options.FilePath);
        Assert.Equal(1000, options.MaxPersistentFieldLength);
        Assert.Equal("http://graylog", options.GrayLog.Address);
        Assert.Equal(12345, options.GrayLog.Port);
        Assert.Equal(2, options.GrayLog.SinkRefreshIntervalMinutes);
        Assert.Equal(LogLevel.Error, options.LevelOverrides["Microsoft.AspNetCore"]);
        Assert.Equal(LogLevel.Debug, options.LevelOverrides["MyApp"]);
    }

    [Theory]
    [InlineData("Verbose", LogLevel.Trace)]
    [InlineData("VERBOSE", LogLevel.Trace)]
    [InlineData("Trace", LogLevel.Trace)]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Fatal", LogLevel.Critical)]
    [InlineData("Critical", LogLevel.Critical)]
    public void LevelNames_MaySerilogsOrMicrosofts_InAnyCase(string text, LogLevel expected)
    {
        Assert.Equal(expected, PersistentLoggerOptions.FromConfiguration(Configuration(("LogLevel", text))).LogLevel);
    }

    [Theory]
    [InlineData("loud")]
    [InlineData("")]
    [InlineData("42")]
    public void UnreadableLevel_KeepsTheDefault(string text)
    {
        Assert.Equal(LogLevel.Information, PersistentLoggerOptions.FromConfiguration(Configuration(("LogLevel", text))).LogLevel);
    }

    [Fact]
    public void UnreadableValues_KeepTheirDefaults()
    {
        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(Configuration(
            ("IsGrayLogActive", "yes"),
            ("IsSelfLogActive", "maybe"),
            ("FilePath", "  "),
            ("GrayLog:Port", "twelve"),
            ("GrayLog:SinkRefreshIntervalMinutes", "0"),
            ("LevelOverrides:Microsoft", "loud")));

        Assert.False(options.IsGrayLogActive);
        Assert.False(options.IsSelfLogActive);
        Assert.Equal("Logs/logs.txt", options.FilePath);
        Assert.Equal(12201, options.GrayLog.Port);
        Assert.Equal(5, options.GrayLog.SinkRefreshIntervalMinutes);
        Assert.Empty(options.LevelOverrides);
    }

    [Fact]
    public void TheConsole_IsToldByLoggingLogLevel_NotByThePersistentLoggerSection()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Error",
            ["Logging:LogLevel:Orders"] = "Debug",
            ["PersistentLogger:LogLevel"] = "Information",
        }).Build();

        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(configuration);

        Assert.Equal(LogLevel.Warning, options.ConsoleLevels.Default);
        Assert.Equal(LogLevel.Error, options.ConsoleLevels.For("Microsoft.AspNetCore.Routing"));
        Assert.Equal(LogLevel.Debug, options.ConsoleLevels.For("Orders.Checkout"));
        Assert.Equal(LogLevel.Warning, options.ConsoleLevels.For("Somewhere.Else"));
        Assert.Equal(LogLevel.Information, options.LogLevel);
    }

    [Fact]
    public void WithoutLoggingLogLevel_TheConsoleShowsInformationAndAbove()
    {
        PersistentLoggerOptions options = PersistentLoggerOptions.FromConfiguration(new ConfigurationBuilder().Build());

        Assert.Equal(LogLevel.Information, options.ConsoleLevels.Default);
        Assert.Equal(LogLevel.Information, options.ConsoleLevels.For("Any.Category"));
    }

    [Fact]
    public void PlatformMinimumLevel_IsRead()
    {
        Assert.Equal(LogLevel.Error, PersistentLoggerOptions.FromConfiguration(Configuration(("PlatformMinimumLevel", "Error"))).PlatformMinimumLevel);
    }

    [Fact]
    public void NullConfiguration_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PersistentLoggerOptions.FromConfiguration(null!));
    }
}
