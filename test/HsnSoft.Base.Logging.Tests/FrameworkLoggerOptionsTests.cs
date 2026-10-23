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
        Assert.False(options.IsGrayLogActive);
        Assert.False(options.IsSelfLogActive);
        Assert.Equal("Logs/logs.txt", options.FilePath);
        Assert.Null(options.GrayLog.Address);
        Assert.Equal(12201, options.GrayLog.Port);
        Assert.Equal(5, options.GrayLog.SinkRefreshIntervalMinutes);
        Assert.Empty(options.LevelOverrides);
    }

    [Fact]
    public void Values_AreRead()
    {
        FrameworkLoggerOptions options = FrameworkLoggerOptions.FromConfiguration(Configuration(
            ("LogLevel", "Information"),
            ("PersistentMinimumLevel", "Error"),
            ("IsGrayLogActive", "true"),
            ("IsSelfLogActive", "True"),
            ("FilePath", "logs/app.txt"),
            ("GrayLog:Address", "http://graylog"),
            ("GrayLog:Port", "12345"),
            ("GrayLog:SinkRefreshIntervalMinutes", "2"),
            ("LevelOverrides:Microsoft.AspNetCore", "Error"),
            ("LevelOverrides:MyApp", "Debug")));

        Assert.Equal(LogLevel.Information, options.LogLevel);
        Assert.Equal(LogLevel.Error, options.PersistentMinimumLevel);
        Assert.True(options.IsGrayLogActive);
        Assert.True(options.IsSelfLogActive);
        Assert.Equal("logs/app.txt", options.FilePath);
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

    [Fact]
    public void NullConfiguration_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkLoggerOptions.FromConfiguration(null!));
    }
}
