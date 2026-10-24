using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class FrameworkLoggerOptionsTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => $"FrameworkLogger:{x.Key}", x => (string?)x.Value)).Build();

    [Fact]
    public void Defaults_AreSafe()
    {
        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(Configuration());

        Assert.Equal(LogLevel.Trace, options.LogLevel);
        Assert.Equal(LogLevel.Information, options.PersistentMinimumLevel);
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
        => Assert.Equal(16384, FrameworkLoggerOptions.FromConfiguration(Configuration(("MaxPersistentFieldLength", text))).MaxPersistentFieldLength);

    [Fact]
    public void TheVersion_IsTheSetting_ThenTheEnvironmentVariable_ThenTheVersionOfTheAssembly()
    {
        Assembly assembly = typeof(FrameworkLoggerOptionsTests).Assembly;
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        static string? Environment(string name) => name == "BUILD_NUMBER" ? "2610241530-sha-ab12cd3" : name == "MY_VERSION" ? "7" : null;

        Assert.Equal("1.2.3", new FrameworkLoggerOptions { Version = "1.2.3" }.ResolveVersion(Environment, assembly));
        Assert.Equal("2610241530-sha-ab12cd3", new FrameworkLoggerOptions().ResolveVersion(Environment, assembly));
        Assert.Equal("7", new FrameworkLoggerOptions { VersionVariable = "MY_VERSION" }.ResolveVersion(Environment, assembly));
        Assert.Equal(informational, new FrameworkLoggerOptions().ResolveVersion(_ => null, assembly));
        Assert.Equal(informational, new FrameworkLoggerOptions().ResolveVersion(_ => " ", assembly));
        Assert.NotNull(new FrameworkLoggerOptions().ResolveVersion());
    }

    [Fact]
    public void TheVersionSettings_AreRead()
    {
        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(Configuration(("Version", "9.9"), ("VersionVariable", "IMAGE_TAG")));
        FrameworkLoggerOptions defaults = FrameworkLoggerOptions.FromConfiguration(Configuration());

        Assert.Equal("9.9", options.Version);
        Assert.Equal("IMAGE_TAG", options.VersionVariable);
        Assert.Null(defaults.Version);
        Assert.Equal("BUILD_NUMBER", defaults.VersionVariable);
    }

    [Fact]
    public void Values_AreRead()
    {
        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(Configuration(
            ("LogLevel", "Information"),
            ("PersistentMinimumLevel", "Error"),
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

        Assert.Equal(LogLevel.Information, options.LogLevel);
        Assert.Equal(LogLevel.Error, options.PersistentMinimumLevel);
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
        Assert.Equal(expected, FrameworkLoggerOptions.FromConfiguration(Configuration(("LogLevel", text))).LogLevel);
    }

    [Theory]
    [InlineData("loud")]
    [InlineData("")]
    [InlineData("42")]
    public void UnreadableLevel_KeepsTheDefault(string text)
    {
        Assert.Equal(LogLevel.Trace, FrameworkLoggerOptions.FromConfiguration(Configuration(("LogLevel", text))).LogLevel);
        Assert.Equal(LogLevel.Information, FrameworkLoggerOptions.FromConfiguration(Configuration(("PersistentMinimumLevel", text))).PersistentMinimumLevel);
    }

    [Fact]
    public void UnreadableValues_KeepTheirDefaults()
    {
        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(Configuration(
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

    [Theory]
    [InlineData(LogLevel.Trace, LogLevel.Information, LogLevel.Information)]
    [InlineData(LogLevel.Trace, LogLevel.Debug, LogLevel.Information)]
    [InlineData(LogLevel.Trace, LogLevel.Trace, LogLevel.Information)]
    [InlineData(LogLevel.Debug, LogLevel.Information, LogLevel.Information)]
    [InlineData(LogLevel.Warning, LogLevel.Information, LogLevel.Warning)]
    [InlineData(LogLevel.Trace, LogLevel.Error, LogLevel.Error)]
    [InlineData(LogLevel.Critical, LogLevel.Information, LogLevel.Critical)]
    public void PersistentLevel_IsNeverBelowInformation_NorBelowTheConfiguredLevel(LogLevel configured, LogLevel persistent, LogLevel expected)
    {
        FrameworkLoggerOptions options = new() { LogLevel = configured, PersistentMinimumLevel = persistent };

        Assert.Equal(expected, options.EffectivePersistentLevel);
    }

    [Theory]
    [InlineData(LogLevel.Information, LogLevel.Information, LogLevel.Information, LogLevel.Information)]
    [InlineData(LogLevel.Warning, LogLevel.Information, LogLevel.Information, LogLevel.Warning)]
    [InlineData(LogLevel.Warning, LogLevel.Error, LogLevel.Information, LogLevel.Error)]
    [InlineData(LogLevel.Trace, LogLevel.Information, LogLevel.Error, LogLevel.Error)]
    public void PlatformLevel_IsNeverBelowThePersistentLevel(LogLevel platform, LogLevel persistent, LogLevel configured, LogLevel expected)
    {
        FrameworkLoggerOptions options = new() { LogLevel = configured, PersistentMinimumLevel = persistent, PlatformMinimumLevel = platform };

        Assert.Equal(expected, options.EffectivePlatformLevel);
        Assert.Equal(LogLevel.Error, FrameworkLoggerOptions.FromConfiguration(Configuration(("PlatformMinimumLevel", "Error"))).PlatformMinimumLevel);
    }

    [Fact]
    public void NullConfiguration_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkLoggerOptions.FromConfiguration(null!));
    }
}
