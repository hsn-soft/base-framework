using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaTests
{
    private sealed record OrderPlaced : IIntegrationEventMessage;

    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public void Options_HaveSafeDefaults()
    {
        var options = new KafkaOptions();

        Assert.Equal("localhost:9092", options.BootstrapServers);
        Assert.Equal("hsnsoft.", options.TopicPrefix);
        Assert.Equal(1, options.Partitions);
        Assert.Equal((short)1, options.ReplicationFactor);
    }

    [Fact]
    public void Topics_AreNamedAfterThePrefixAndTheEvent()
    {
        var options = new KafkaOptions { TopicPrefix = "acme." };

        Assert.Equal("acme.OrderPlaced", KafkaTopology.TopicFor(options, "OrderPlaced"));
        Assert.Equal("acme.dead-letter", KafkaTopology.DeadLetterTopic(options));
    }

    [Fact]
    public void Registration_BindsTheSection_AndNamesTheService()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddKafkaEventBus(
            Configuration(
                ("Kafka:BootstrapServers", "broker:9093"),
                ("Kafka:TopicPrefix", "acme."),
                ("Kafka:EventBus:ExchangeName", "acme"),
                ("Kafka:EventBus:ConsumerClientName", "order-service")),
            options => options.MaxHandlerAttempts = 7);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("broker:9093", provider.GetRequiredService<IOptions<KafkaOptions>>().Value.BootstrapServers);
        Assert.Equal("acme.", provider.GetRequiredService<IOptions<KafkaOptions>>().Value.TopicPrefix);
        Assert.Equal("acme_order-service", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ConsumerClientInfo);
        Assert.Equal(7, provider.GetRequiredService<IOptions<EventBusOptions>>().Value.MaxHandlerAttempts);
        Assert.IsType<KafkaEventBus>(provider.GetRequiredService<IEventBus>());
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is KafkaSubscriber);
    }

    [Fact]
    public void Registration_AddsAReadinessCheck()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddKafkaEventBus(Configuration());
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("kafka", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public async Task HealthCheck_IsUnhealthy_WhenTheBrokersCannotBeReached()
    {
        var check = new KafkaHealthCheck(Options.Create(new KafkaOptions { BootstrapServers = "127.0.0.1:1" }));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Subscriber_WithoutSubscriptions_ReturnsImmediately()
    {
        InMemoryEventBusSubscriptionManager subscriptions = new([], Options.Create(new EventBusOptions()), NullLogger<InMemoryEventBusSubscriptionManager>.Instance);
        var subscriber = new KafkaSubscriber(
            subscriptions,
            new EventDispatcher(new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), subscriptions, Options.Create(new EventBusOptions()), NullLogger<EventDispatcher>.Instance),
            Options.Create(new KafkaOptions()),
            Options.Create(new EventBusOptions()),
            NullLogger<KafkaSubscriber>.Instance);

        await subscriber.StartAsync(CancellationToken.None);
        await (subscriber.ExecuteTask ?? Task.CompletedTask);
        await subscriber.StopAsync(CancellationToken.None);

        Assert.True(subscriber.ExecuteTask is { IsCompletedSuccessfully: true });
    }

    [Fact]
    public void EventBus_CanBeDisposedBeforeItPublishedAnything()
    {
        var bus = new KafkaEventBus(Options.Create(new KafkaOptions()), new EventEnvelopeFactory(Options.Create(new EventBusOptions()), []), Options.Create(new EventBusOptions()), NullLogger<KafkaEventBus>.Instance);

        Assert.Null(Record.Exception(bus.Dispose));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => KafkaServiceCollectionExtensions.AddKafkaEventBus(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddKafkaEventBus(null!));
    }
}
