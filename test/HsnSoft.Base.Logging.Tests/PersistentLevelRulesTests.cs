using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class PersistentLevelRulesTests
{
    [Theory]
    [InlineData(LogLevel.Trace, "Orders.X", false, LogLevel.Information)]
    [InlineData(LogLevel.Debug, "Orders.X", false, LogLevel.Information)]
    [InlineData(LogLevel.Information, "Orders.X", false, LogLevel.Information)]
    [InlineData(LogLevel.Warning, "Orders.X", false, LogLevel.Warning)]
    [InlineData(LogLevel.Critical, "Orders.X", false, LogLevel.Critical)]
    public void TheLevel_IsPersistentLoggerLogLevel_NeverBelowInformation(LogLevel configured, string category, bool isPlatform, LogLevel expected)
    {
        PersistentLevelRules rules = new(new PersistentLoggerOptions { LogLevel = configured });

        Assert.Equal(expected, rules.For(category, isPlatform));
    }

    [Theory]
    [InlineData(LogLevel.Information, LogLevel.Warning, LogLevel.Warning)]
    [InlineData(LogLevel.Information, LogLevel.Information, LogLevel.Information)]
    [InlineData(LogLevel.Error, LogLevel.Warning, LogLevel.Error)]
    public void ThePlatformsLogs_NeedAtLeastThePlatformMinimum_AndNeverLessThanTheLogLevel(LogLevel logLevel, LogLevel platformMinimum, LogLevel expected)
    {
        PersistentLevelRules rules = new(new PersistentLoggerOptions { LogLevel = logLevel, PlatformMinimumLevel = platformMinimum });

        Assert.Equal(expected, rules.For("Microsoft.AspNetCore.Routing", isPlatform: true));
        Assert.Equal(logLevel, rules.For("Orders.X", isPlatform: false));
    }

    [Fact]
    public void ANamedCategory_HasItsOwnLevel_EvenForThePlatform_ButNotBelowInformation()
    {
        PersistentLoggerOptions options = new() { LogLevel = LogLevel.Warning };
        options.LevelOverrides["Microsoft.AspNetCore.Hosting"] = LogLevel.Information;
        options.LevelOverrides["Orders.Noisy"] = LogLevel.Error;
        options.LevelOverrides["Orders.Quiet"] = LogLevel.Debug;
        PersistentLevelRules rules = new(options);

        Assert.Equal(LogLevel.Information, rules.For("Microsoft.AspNetCore.Hosting.Diagnostics", isPlatform: true));
        Assert.Equal(LogLevel.Warning, rules.For("Microsoft.AspNetCore.Routing", isPlatform: true));
        Assert.Equal(LogLevel.Error, rules.For("Orders.Noisy.Part", isPlatform: false));
        Assert.Equal(LogLevel.Information, rules.For("Orders.Quiet", isPlatform: false));
        Assert.Throws<ArgumentNullException>(() => rules.For(null!, false));
    }
}
