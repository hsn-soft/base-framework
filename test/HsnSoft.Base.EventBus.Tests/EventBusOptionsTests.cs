namespace HsnSoft.Base.EventBus.Tests;

public class EventBusOptionsTests
{
    [Fact]
    public void Defaults_AreTheOnesOfTheProductionFramework()
    {
        EventBusOptions options = new();

        Assert.Equal("HsnSoft", options.ExchangeName);
        Assert.Equal("Client", options.ConsumerClientName);
        Assert.Null(options.ErrorClientName);
        Assert.Equal(string.Empty, options.EventNamePrefix);
        Assert.Equal("Eto", options.EventNameSuffix);
        Assert.Equal(1, options.MaxHandlerAttempts);
        Assert.Equal(5, options.PublishRetryCount);
    }

    [Fact]
    public void ClientInfo_IsTheExchangeAndTheClientName()
    {
        EventBusOptions options = new() { ExchangeName = "HHS", ConsumerClientName = "IdentityService", ErrorClientName = "EventManagerService" };

        Assert.Equal("HHS_IdentityService", options.ConsumerClientInfo);
        Assert.Equal("HHS_EventManagerService", options.ErrorClientInfo);
    }

    [Fact]
    public void ErrorClient_DefaultsToTheConsumerItself()
    {
        EventBusOptions options = new() { ExchangeName = "HHS", ConsumerClientName = "IdentityService" };

        Assert.Equal("HHS_IdentityService", options.ErrorClientInfo);
    }

    [Theory]
    [InlineData("", "Svc", "Svc", "Svc")]
    [InlineData("HHS", "", "HHS_", "HHS_")]
    [InlineData("", "", "", "")]
    public void MissingParts_AreLeftOut(string exchange, string client, string consumerInfo, string errorInfo)
    {
        EventBusOptions options = new() { ExchangeName = exchange, ConsumerClientName = client };

        Assert.Equal(consumerInfo, options.ConsumerClientInfo);
        Assert.Equal(errorInfo, options.ErrorClientInfo);
    }
}
