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

    private Task<IHost> StartAsync(string exchange, Action<IServiceCollection> handlers, string? service = null, TimeSpan? shutdownTimeout = null)
    {
        IConfiguration configuration = Support.Configuration(
            ("Kafka:BootstrapServers", _container.GetBootstrapAddress()),
            ("Kafka:EventBus:ExchangeName", exchange),
            ("Kafka:EventBus:ConsumerClientName", service ?? $"service-{Guid.NewGuid():N}"));
        return Support.StartHostAsync(services =>
        {
            services.AddKafkaEventBus(configuration, options =>
            {
                options.MaxHandlerAttempts = 2;
                options.RetryDelay = TimeSpan.FromMilliseconds(10);
                options.ShutdownTimeout = shutdownTimeout ?? options.ShutdownTimeout;
            });
            handlers(services);
        });
    }

    [Fact]
    public async Task PublishedEvent_ReachesTheHandler_AndTheServiceIsReady()
    {
        var recorder = new Recorder();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("hello"));
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("second"));

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count >= 2), "both events to be handled");
        Assert.Contains(recorder.Received, envelope => envelope.Message.Text == "hello");
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
        await host.StopAsync();
    }

    [Fact]
    public async Task EventTheHandlerCannotHandle_EndsInTheDeadLetterTopic()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        using IHost host = await StartAsync(exchange, services => services.AddEventHandler<Pinged, FailingHandler>());
        string text = $"doomed-{Guid.NewGuid():N}";
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged(text));

        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _container.GetBootstrapAddress(),
            GroupId = $"probe-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe($"{exchange}_dead-letter");

        await Support.EventuallyAsync(() =>
        {
            ConsumeResult<string, byte[]> message = consumer.Consume(TimeSpan.FromSeconds(1));
            return Task.FromResult(message is not null && System.Text.Encoding.UTF8.GetString(message.Message.Value).Contains(text, StringComparison.Ordinal)
                                   && System.Text.Encoding.UTF8.GetString(message.Message.Headers.GetLastBytes("x-original-topic")) == $"{exchange}_Pinged");
        }, "the message on the dead-letter topic");
        await host.StopAsync();
    }

    [Fact]
    public async Task TwoServices_ReadTheSameEventEachWithItsOwnOffset()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        Recorder first = new();
        Recorder second = new();
        using IHost aaa = await StartAsync(exchange, services =>
        {
            services.AddSingleton(first);
            services.AddEventHandler<Pinged, RecordingHandler>();
        }, "aaa_service");
        using IHost bbb = await StartAsync(exchange, services =>
        {
            services.AddSingleton(second);
            services.AddEventHandler<Pinged, RecordingHandler>();
        }, "bbb_service");

        await aaa.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("for both"));

        await Support.EventuallyAsync(() => Task.FromResult(first.Received.Count == 1 && second.Received.Count == 1), "both services to handle the event");
        await aaa.StopAsync();
        await bbb.StopAsync();
    }

    [Fact]
    public async Task ASlowEvent_DoesNotHoldBackAnotherOneOfTheSameService()
    {
        Recorder recorder = new();
        BlockedGate gate = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(recorder);
            services.AddSingleton(gate);
            services.AddEventHandler<Pinged, BlockedHandler>();
            services.AddEventHandler<Ponged, FollowUpHandler>();
        });
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();

        await bus.PublishAsync(new Pinged("stuck"));
        await bus.PublishAsync(new Ponged("passes by"));

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Followed.Count == 1), "the other event to be handled while the first one waits");
        Assert.Empty(recorder.Received);

        gate.Released.SetResult();
        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 1), "the first event to be handled once it is released");
        await host.StopAsync();
    }

    [Fact]
    public async Task ASevice_ThatStops_LetsTheMessageItIsHandlingFinish()
    {
        StopProbe probe = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(probe);
            services.AddEventHandler<Pinged, LongRunningHandler>();
        });
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("in flight"));
        await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // the handler needs three seconds; the host is asked to stop one second into them
        await host.StopAsync();

        Assert.True(probe.Completed, "the message being handled was not allowed to finish");
        Assert.False(probe.Cancelled);
    }

    [Fact]
    public async Task ASevice_ThatStops_CancelsAHandlerThatIsStillWorkingWhenTheShutdownTimeoutHasPassed()
    {
        StopProbe probe = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(probe);
            services.AddEventHandler<Pinged, LongRunningHandler>();
        }, shutdownTimeout: TimeSpan.FromMilliseconds(500));
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("too slow"));
        await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await host.StopAsync();

        Assert.True(probe.Cancelled, "the handler was not cancelled after the shutdown timeout");
        Assert.False(probe.Completed);
    }
}
