using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class RabbitMqTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    private static RabbitMqConnection NewConnection()
        => new(Options.Create(new RabbitMqOptions()), NullLogger<RabbitMqConnection>.Instance);

    [Fact]
    public void Options_HaveSafeDefaults()
    {
        var options = new RabbitMqOptions();

        Assert.Equal("localhost", options.Host);
        Assert.Equal(5672, options.Port);
        Assert.Equal("guest", options.UserName);
        Assert.Equal("guest", options.Password);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal("hsnsoft.events", options.ExchangeName);
    }

    [Fact]
    public void DeadLetterNames_AreDerivedFromTheExchange()
    {
        var options = new RabbitMqOptions { ExchangeName = "acme.events" };

        Assert.Equal("acme.events.dlx", RabbitMqTopology.DeadLetterExchange(options));
        Assert.Equal("acme.events.dead-letter", RabbitMqTopology.DeadLetterQueue(options));
    }

    [Fact]
    public async Task Registration_BindsTheSection_AndNamesTheService()
    {
        var services = new ServiceCollection().AddLogging().AddRabbitMqEventBus(
            Configuration(("RabbitMq:Host", "broker"), ("RabbitMq:ExchangeName", "acme.events")),
            "order-service",
            options => options.RetryDelay = TimeSpan.FromSeconds(1));
        await using var provider = services.BuildServiceProvider();

        Assert.Equal("broker", provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value.Host);
        Assert.Equal("order-service", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ServiceName);
        Assert.Equal(TimeSpan.FromSeconds(1), provider.GetRequiredService<IOptions<EventBusOptions>>().Value.RetryDelay);
        Assert.IsType<RabbitMqEventBus>(provider.GetRequiredService<IEventBus>());
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is RabbitMqSubscriber);
    }

    [Fact]
    public async Task HealthCheck_IsUnhealthy_UntilAConnectionIsOpen()
    {
        await using var connection = NewConnection();
        var check = new RabbitMqHealthCheck(connection);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.False(connection.IsConnected);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Registration_AddsAReadinessCheck()
    {
        var services = new ServiceCollection().AddLogging().AddRabbitMqEventBus(Configuration(), "svc");
        await using var provider = services.BuildServiceProvider();

        var registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("rabbitmq", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public async Task Subscriber_WithoutSubscriptions_ReturnsImmediately()
    {
        await using var connection = NewConnection();
        var subscriber = new RabbitMqSubscriber(
            connection,
            [],
            new EventDispatcher(new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), Options.Create(new EventBusOptions()), NullLogger<EventDispatcher>.Instance),
            Options.Create(new RabbitMqOptions()),
            Options.Create(new EventBusOptions()),
            NullLogger<RabbitMqSubscriber>.Instance);

        await subscriber.StartAsync(CancellationToken.None);
        await subscriber.StopAsync(CancellationToken.None);

        Assert.False(connection.IsConnected);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => RabbitMqServiceCollectionExtensions.AddRabbitMqEventBus(null!, Configuration(), "svc"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddRabbitMqEventBus(null!, "svc"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddRabbitMqEventBus(Configuration(), " "));
    }
}
