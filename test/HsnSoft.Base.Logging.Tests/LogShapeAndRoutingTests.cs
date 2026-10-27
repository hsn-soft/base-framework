using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class LogShapeAndRoutingTests
{
    [Theory]
    [InlineData("", "AppLog")]
    [InlineData("ConsoleOnly.Startup", "AppLog")]
    [InlineData("HsnSoft.Base.EventBus.EventDispatcher", "FrameworkLog")]
    [InlineData("HsnSoft.Base", "FrameworkLog")]
    [InlineData("Abc.Shared.Hosting.Thing", "SharedLog")]
    [InlineData("Abc.OrderService.Application.OrderAppService", "AppLog")]
    [InlineData("Abc", "AppLog")]
    [InlineData("AbcExtras.Thing", "PlatformLog")]
    [InlineData("Microsoft.AspNetCore.Routing", "PlatformLog")]
    public void TheTypeOfALog_IsToldFromItsCategory(string category, string expected)
        => Assert.Equal(expected, LogShape.LogType(category, "Abc"));

    [Fact]
    public void TheTypeNeedsACategoryAndAnApplicationNamespace()
    {
        Assert.Throws<ArgumentNullException>(() => LogShape.LogType(null!, "Abc"));
        Assert.Throws<ArgumentException>(() => LogShape.LogType("x", " "));
    }

    [Theory]
    [InlineData("CONSUME_EVENT_SUCCESS", "CONSUME_EVENT_SUCCESS")]
    [InlineData("HTTP_2", "HTTP_2")]
    [InlineData("ExecutedEndpoint", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void OnlyTheNamesInCapitals_AreFacilities(string? eventName, string? facility)
        => Assert.Equal(facility, LogShape.FacilityName(eventName));

    [Fact]
    public void TheNoise_DependsOnTheTypeOfTheLog()
    {
        Assert.Equal([LogShape.ConnectionIdField, .. LogFields.WhoFields], LogShape.NoiseFields(LogTypes.FrameworkLog));
        Assert.Equal(LogShape.NoiseFields(LogTypes.FrameworkLog), LogShape.NoiseFields(LogTypes.PlatformLog));
        Assert.Contains("RequestPath", LogShape.NoiseFields(LogTypes.AppLog));
        Assert.Equal(LogShape.NoiseFields(LogTypes.AppLog), LogShape.NoiseFields(LogTypes.SharedLog));
        Assert.Equal([LogShape.ConnectionIdField], LogShape.NoiseFields("RequestLog"));
    }

    [Fact]
    public void TheConsole_ShowsWhatTheLevelsSay_AndNeverWhatIsAboveNone()
    {
        LogLevelRules rules = new(LogLevel.Information, [new("Microsoft", LogLevel.Warning)]);

        Assert.True(LogRouting.ForConsole(LogLevel.Information, "Abc.Thing", rules));
        Assert.False(LogRouting.ForConsole(LogLevel.Debug, "Abc.Thing", rules));
        Assert.False(LogRouting.ForConsole(LogLevel.Information, "Microsoft.AspNetCore", rules));
        Assert.True(LogRouting.ForConsole(LogLevel.Warning, "Microsoft.AspNetCore", rules));
        Assert.Throws<ArgumentNullException>(() => LogRouting.ForConsole(LogLevel.Error, "x", null!));
    }

    [Fact]
    public void ThePersistentSink_GetsWhatItsLevelsSay_ExceptTheConsoleOnlyLogsAndTheNoiseOfThePlatform()
    {
        PersistentLevelRules rules = new(new PersistentLoggerOptions { LogLevel = LogLevel.Information, PlatformMinimumLevel = LogLevel.Warning });

        Assert.True(LogRouting.ForPersistent(LogLevel.Information, "Abc.Thing", LogTypes.AppLog, rules));
        Assert.False(LogRouting.ForPersistent(LogLevel.Debug, "Abc.Thing", LogTypes.AppLog, rules));
        Assert.False(LogRouting.ForPersistent(LogLevel.Information, "Microsoft.AspNetCore", LogTypes.PlatformLog, rules));
        Assert.True(LogRouting.ForPersistent(LogLevel.Warning, "Microsoft.AspNetCore", LogTypes.PlatformLog, rules));
        Assert.False(LogRouting.ForPersistent(LogLevel.Critical, "ConsoleOnly.Startup", LogTypes.AppLog, rules));
        Assert.True(LogRouting.IsConsoleOnly("ConsoleOnly.Startup"));
        Assert.False(LogRouting.IsConsoleOnly("Abc"));
        Assert.Throws<ArgumentNullException>(() => LogRouting.ForPersistent(LogLevel.Error, "x", "AppLog", null!));
        Assert.Throws<ArgumentNullException>(() => LogRouting.IsConsoleOnly(null!));
    }

    private sealed class Account
    {
        public string Name { get; set; } = "alice";

        [SensitiveData(SubstituteText = "***")]
        public string? Password { get; set; } = "secret";

        public List<int> Numbers { get; set; } = [1, 2];

        public Dictionary<string, string> Labels { get; set; } = new() { ["a"] = "b" };

        public Account? Self { get; set; }

        public Guid Id { get; set; } = Guid.Empty;
    }

    [Fact]
    public void AnObject_BecomesATreeWithTheSecretsMasked()
    {
        Account account = new();
        account.Self = account;

        MaskedStructure tree = Assert.IsType<MaskedStructure>(SensitiveDataTree.Build(account));
        Dictionary<string, MaskedNode> properties = tree.Properties.ToDictionary(property => property.Key, property => property.Value);

        Assert.Equal("Account", tree.TypeName);
        Assert.Equal("alice", ((MaskedScalar)properties["Name"]).Value);
        Assert.Equal("***", ((MaskedScalar)properties["Password"]).Value);
        Assert.Equal(2, ((MaskedSequence)properties["Numbers"]).Items.Count);
        Assert.Equal("b", ((MaskedScalar)((MaskedDictionary)properties["Labels"]).Entries.Single().Value).Value);
        Assert.Equal("[CyclicRef:Account]", ((MaskedScalar)properties["Self"]).Value);
        Assert.Equal(Guid.Empty, ((MaskedScalar)properties["Id"]).Value);
        Assert.Null(((MaskedScalar)SensitiveDataTree.Build(null)).Value);
        Assert.True(SensitiveDataTree.IsSimple(typeof(Guid)));
        Assert.False(SensitiveDataTree.IsSimple(typeof(Account)));
    }
}
