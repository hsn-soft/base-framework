using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class RabbitMqRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    private static ServiceProvider Build(IConfiguration configuration, string connection = "RabbitMq:Connection", string eventBus = "RabbitMq:EventBus", Action<EventBusOptions>? configure = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddRabbitMqEventBus(configuration, connection, eventBus, configure);
        services.AddEventHandler<SampleEto, SampleHandler>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Sections_AreBoundToTheirOptions()
    {
        await using ServiceProvider provider = Build(Configuration(
            ("RabbitMq:Connection:HostName", "broker"),
            ("RabbitMq:Connection:Port", "5673"),
            ("RabbitMq:Connection:ConnectionRetryCount", "9"),
            ("RabbitMq:EventBus:ExchangeName", "HHS"),
            ("RabbitMq:EventBus:ConsumerClientName", "IdentityService"),
            ("RabbitMq:EventBus:ErrorClientName", "EventManagerService"),
            ("RabbitMq:EventBus:EventNameSuffix", "Event"),
            ("RabbitMq:EventBus:RequeueDelay", "00:00:07")));

        RabbitMqConnectionSettings connection = provider.GetRequiredService<IOptions<RabbitMqConnectionSettings>>().Value;
        EventBusOptions eventBus = provider.GetRequiredService<IOptions<EventBusOptions>>().Value;
        RabbitMqEventBusConfig tuning = provider.GetRequiredService<IOptions<RabbitMqEventBusConfig>>().Value;
        Assert.Equal("broker", connection.HostName);
        Assert.Equal(5673, connection.Port);
        Assert.Equal(9, connection.ConnectionRetryCount);
        Assert.Equal("HHS_IdentityService", eventBus.ConsumerClientInfo);
        Assert.Equal("HHS_EventManagerService", eventBus.ErrorClientInfo);
        Assert.Equal("Event", eventBus.EventNameSuffix);
        Assert.Equal(TimeSpan.FromSeconds(7), tuning.RequeueDelay);
    }

    [Fact]
    public async Task SectionNames_CanBeChosen()
    {
        await using ServiceProvider provider = Build(
            Configuration(("Broker:Link:HostName", "elsewhere"), ("Broker:Bus:ExchangeName", "Custom")),
            connection: "Broker:Link",
            eventBus: "Broker:Bus");

        Assert.Equal("elsewhere", provider.GetRequiredService<IOptions<RabbitMqConnectionSettings>>().Value.HostName);
        Assert.Equal("Custom", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ExchangeName);
    }

    [Fact]
    public async Task CodeConfiguration_RunsAfterTheConfiguration()
    {
        await using ServiceProvider provider = Build(
            Configuration(("RabbitMq:EventBus:ConsumerClientName", "FromConfiguration")),
            configure: options => options.ConsumerClientName = "FromCode");

        Assert.Equal("FromCode", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ConsumerClientName);
    }

    [Fact]
    public async Task Services_AreRegistered()
    {
        await using ServiceProvider provider = Build(Configuration());

        Assert.IsType<EventBusRabbitMq>(provider.GetRequiredService<IEventBus>());
        Assert.IsType<FailedEventNotifier>(provider.GetRequiredService<IFailedEventNotifier>());
        Assert.IsType<RabbitMqPersistentConnection>(provider.GetRequiredService<IRabbitMqPersistentConnection>());
        Assert.IsType<InMemoryEventBusSubscriptionManager>(provider.GetRequiredService<IEventBusSubscriptionManager>());
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is RabbitMqSubscriber);
        Assert.True(provider.GetRequiredService<IEventBusSubscriptionManager>().HasSubscriptionsForEvent("Sample"));
    }

    [Fact]
    public async Task ReadinessCheck_IsRegistered()
    {
        await using ServiceProvider provider = Build(Configuration());

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("rabbitmq", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public async Task HealthCheck_IsHealthy_ByTheConnectionTheServiceHas_AndOpensNoOther()
    {
        FakeConnection connection = new() { IsConnected = true };

        HealthCheckResult result = await new RabbitMqHealthCheck(connection).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(0, connection.ConnectCalls);
    }

    [Fact]
    public async Task HealthCheck_ConnectsTheOneConnectionOfAServiceThatHasNotConnectedYet()
    {
        FakeConnection connection = new() { IsConnected = false, ConnectSucceeds = true };

        HealthCheckResult result = await new RabbitMqHealthCheck(connection).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(1, connection.ConnectCalls);
    }

    [Fact]
    public async Task HealthCheck_IsUnhealthy_WhenTheConnectionIsLostAndNotBackYet()
    {
        FakeConnection connection = new() { IsConnected = false, ConnectSucceeds = false };

        HealthCheckResult result = await new RabbitMqHealthCheck(connection).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.False(string.IsNullOrEmpty(result.Description));
    }

    [Fact]
    public async Task HealthCheck_IsUnhealthy_WhenTheBrokerCannotBeReached_AndTheDescriptionHasNoAddress()
    {
        FakeConnection connection = new() { IsConnected = false, ConnectFailure = new InvalidOperationException("broker-7.internal:5672 refused") };

        HealthCheckResult result = await new RabbitMqHealthCheck(connection).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("broker-7", result.Description);
        Assert.Same(connection.ConnectFailure, result.Exception);
    }

    [Fact]
    public async Task HealthCheck_PassesCancellationOn()
    {
        FakeConnection connection = new() { IsConnected = false, ConnectFailure = new OperationCanceledException() };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RabbitMqHealthCheck(connection).CheckHealthAsync(new HealthCheckContext()));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => RabbitMqServiceCollectionExtensions.AddRabbitMqEventBus(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddRabbitMqEventBus(null!));
    }
}
