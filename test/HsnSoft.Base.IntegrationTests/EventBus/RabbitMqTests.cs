using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace HsnSoft.Base.IntegrationTests.EventBus;

public sealed class RabbitMqTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4").WithUsername("guest").WithPassword("guest").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private (string Host, string Port) Endpoint => (_container.Hostname, _container.GetMappedPublicPort(5672).ToString(System.Globalization.CultureInfo.InvariantCulture));

    private Task<IHost> StartAsync(string exchange, Action<IServiceCollection> handlers)
    {
        var configuration = Support.Configuration(
            ("RabbitMq:Host", Endpoint.Host),
            ("RabbitMq:Port", Endpoint.Port),
            ("RabbitMq:ExchangeName", exchange));
        return Support.StartHostAsync(services =>
        {
            services.AddRabbitMqEventBus(configuration, "it-service", options =>
            {
                options.MaxHandlerAttempts = 2;
                options.RetryDelay = TimeSpan.FromMilliseconds(10);
            });
            handlers(services);
        });
    }

    [Fact]
    public async Task PublishedEvent_ReachesTheHandler_AndTheServiceIsReady()
    {
        var recorder = new Recorder();
        using var host = await StartAsync($"it.{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        var bus = host.Services.GetRequiredService<IEventBus>();
        var ping = new Pinged("hello");

        // the subscriber declares its queue in the background; publish until it is bound
        await Support.EventuallyAsync(async () =>
        {
            await bus.PublishAsync(ping);
            await Task.Delay(300);
            return recorder.Received.Any(x => x.EventId == ping.EventId);
        }, "the handler to receive the event");

        Assert.Equal("hello", recorder.Received.First(x => x.EventId == ping.EventId).Text);
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
        await host.StopAsync();
    }

    [Fact]
    public async Task EventTheHandlerCannotHandle_EndsInTheDeadLetterQueue()
    {
        var exchange = $"it.{Guid.NewGuid():N}";
        using var host = await StartAsync(exchange, services => services.AddEventHandler<Pinged, FailingHandler>());
        var bus = host.Services.GetRequiredService<IEventBus>();
        var ping = new Pinged("doomed");

        var factory = new ConnectionFactory { HostName = Endpoint.Host, Port = int.Parse(Endpoint.Port, System.Globalization.CultureInfo.InvariantCulture) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await Support.EventuallyAsync(async () =>
        {
            await bus.PublishAsync(ping);
            await Task.Delay(300);
            var message = await channel.BasicGetAsync($"{exchange}.dead-letter", autoAck: true);
            return message is not null && message.BasicProperties.MessageId == ping.EventId.ToString();
        }, "the message in the dead-letter queue");
        await host.StopAsync();
    }

    [Fact]
    public async Task Connect_GivesUpWhenTheBrokerCannotBeReached_AndWhenCancelled()
    {
        await using var connection = new RabbitMqConnection(
            Options.Create(new RabbitMqOptions { Host = "127.0.0.1", Port = 1 }),
            NullLogger<RabbitMqConnection>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.GetAsync(cancellation.Token));
        Assert.False(connection.IsConnected);
    }
}
