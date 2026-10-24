using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class LogLevelRulesTests
{
    private static readonly LogLevelRules s_rules = new(LogLevel.Information,
    [
        new("Microsoft", LogLevel.Warning),
        new("Microsoft.AspNetCore.Routing", LogLevel.Error),
        new("Orders", LogLevel.Debug),
    ]);

    [Theory]
    [InlineData("Microsoft", LogLevel.Warning)]
    [InlineData("Microsoft.Extensions.Http", LogLevel.Warning)]
    [InlineData("Microsoft.AspNetCore.Routing.Endpoint", LogLevel.Error)]
    [InlineData("microsoft.aspnetcore.routing", LogLevel.Error)]
    [InlineData("Orders.Checkout.OrderService", LogLevel.Debug)]
    [InlineData("MicrosoftExtras.Thing", LogLevel.Information)]
    [InlineData("OrdersExtra", LogLevel.Information)]
    [InlineData("Somewhere.Else", LogLevel.Information)]
    [InlineData("", LogLevel.Information)]
    public void TheLongestRuleThatFits_Wins_AtTheBoundaryOfAName(string category, LogLevel expected)
    {
        Assert.Equal(expected, s_rules.For(category));
    }

    [Fact]
    public void TryFor_SaysWhetherARuleNamesTheCategory()
    {
        Assert.True(s_rules.TryFor("Orders.X", out LogLevel named));
        Assert.Equal(LogLevel.Debug, named);
        Assert.False(s_rules.TryFor("Other", out LogLevel unnamed));
        Assert.Equal(LogLevel.Information, unnamed);
    }

    [Fact]
    public void AreReadFromAConfigurationSection_AsLoggingLogLevelIs()
    {
        IConfigurationSection section = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Error",
            ["Logging:LogLevel:Chatty"] = "None",
            ["Logging:LogLevel:Bad"] = "loud",
            ["Logging:LogLevel:Verbose"] = "Verbose",
        }).Build().GetSection("Logging:LogLevel");

        LogLevelRules rules = LogLevelRules.FromConfiguration(section, LogLevel.Information);

        Assert.Equal(LogLevel.Warning, rules.Default);
        Assert.Equal(LogLevel.Error, rules.For("Microsoft.AspNetCore.Routing"));
        Assert.Equal(LogLevel.None, rules.For("Chatty.Library"));
        // an unreadable level is left out, and Serilog's names are understood
        Assert.Equal(LogLevel.Warning, rules.For("Bad.Thing"));
        Assert.Equal(LogLevel.Trace, rules.For("Verbose.Thing"));
    }

    [Fact]
    public void WithoutADefault_TheFallbackHolds()
    {
        LogLevelRules rules = LogLevelRules.FromConfiguration(new ConfigurationBuilder().Build().GetSection("Logging:LogLevel"), LogLevel.Error);

        Assert.Equal(LogLevel.Error, rules.Default);
        Assert.Throws<ArgumentNullException>(() => LogLevelRules.FromConfiguration(null!, LogLevel.Error));
    }
}
