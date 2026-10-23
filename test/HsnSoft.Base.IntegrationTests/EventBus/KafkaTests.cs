using Confluent.Kafka;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.Kafka;

namespace HsnSoft.Base.IntegrationTests.EventBus;

public sealed class KafkaTests : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.8.0").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private Task<IHost> StartAsync(string prefix, Action<IServiceCollection> handlers)
    {
        IConfiguration configuration = Support.Configuration(
            ("Kafka:BootstrapServers", _container.GetBootstrapAddress()),
            ("Kafka:TopicPrefix", prefix));
        return Support.StartHostAsync(services =>
        {
            services.AddKafkaEventBus(configuration, $"it-{Guid.NewGuid():N}", options =>
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
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}.", services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        var ping = new Pinged("hello");

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(ping);
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("second"));

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count >= 2), "both events to be handled");
        Assert.Equal("hello", recorder.Received.First(x => x.EventId == ping.EventId).Text);
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
        await host.StopAsync();
    }

    [Fact]
    public async Task EventTheHandlerCannotHandle_EndsInTheDeadLetterTopic()
    {
        string prefix = $"it{Guid.NewGuid():N}.";
        using IHost host = await StartAsync(prefix, services => services.AddEventHandler<Pinged, FailingHandler>());
        var ping = new Pinged("doomed");

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(ping);

        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _container.GetBootstrapAddress(),
            GroupId = $"probe-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe($"{prefix}dead-letter");

        await Support.EventuallyAsync(() =>
        {
            ConsumeResult<string, byte[]> message = consumer.Consume(TimeSpan.FromSeconds(1));
            return Task.FromResult(message?.Message.Key == ping.EventId.ToString()
                                   && System.Text.Encoding.UTF8.GetString(message.Message.Headers.GetLastBytes("x-original-topic")) == $"{prefix}Pinged");
        }, "the message on the dead-letter topic");
        await host.StopAsync();
    }
}
