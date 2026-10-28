using Confluent.Kafka;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.Kafka;

namespace HsnSoft.Base.IntegrationTests.EventBus;

public sealed class KafkaTests : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.8.0").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private Task<IHost> StartAsync(string exchange, Action<IServiceCollection> handlers, string? service = null, TimeSpan? shutdownTimeout = null, int partitions = 1, TimeSpan? inProgressRetryDelay = null, string? maxPollInterval = null)
    {
        List<(string Key, string? Value)> settings =
        [
            ("Kafka:BootstrapServers", _container.GetBootstrapAddress()),
            ("Kafka:Partitions", partitions.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Kafka:EventBus:ExchangeName", exchange),
            ("Kafka:EventBus:ConsumerClientName", service ?? $"service-{Guid.NewGuid():N}"),
        ];
        if (maxPollInterval is not null)
        {
            settings.Add(("Kafka:MaxPollInterval", maxPollInterval));
        }

        IConfiguration configuration = Support.Configuration([.. settings]);
        return Support.StartHostAsync(services =>
        {
            services.AddKafkaEventBus(configuration, options =>
            {
                options.MaxHandlerAttempts = 2;
                options.RetryDelay = TimeSpan.FromMilliseconds(10);
                options.ShutdownTimeout = shutdownTimeout ?? options.ShutdownTimeout;
                options.InProgressRetryDelay = inProgressRetryDelay ?? options.InProgressRetryDelay;
            });
            handlers(services);
        });
    }

    [Fact]
    public async Task AMaxPollIntervalNotLongerThanTheShutdownTimeout_StopsTheServiceAtStart()
    {
        // a consumer waits the shutdown timeout for what it handles when partitions move, and would be taken out of its group meanwhile: the misconfiguration stops the host (the default of a failed background service)
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services => services.AddEventHandler<Pinged, RecordingHandler>(), maxPollInterval: "00:00:10");

        await Support.EventuallyAsync(() => Task.FromResult(host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested), "the host to be stopped by the failed subscriber", TimeSpan.FromSeconds(30));
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
    public async Task HandlerThatCancelsItself_EndsInTheDeadLetterTopic_AndTheOffsetIsCommitted()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        string service = $"service-{Guid.NewGuid():N}";
        using IHost host = await StartAsync(exchange, services => services.AddEventHandler<Pinged, SelfCancellingHandler>(), service);
        string text = $"timed-out-{Guid.NewGuid():N}";
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged(text));

        using IConsumer<string, byte[]> probe = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _container.GetBootstrapAddress(),
            GroupId = $"probe-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        probe.Subscribe($"{exchange}_dead-letter");
        await Support.EventuallyAsync(() =>
        {
            ConsumeResult<string, byte[]> message = probe.Consume(TimeSpan.FromSeconds(1));
            return Task.FromResult(message is not null && System.Text.Encoding.UTF8.GetString(message.Message.Value).Contains(text, StringComparison.Ordinal));
        }, "the message on the dead-letter topic");

        // the offset was completed: it is committed, so a service that starts again does not read the message again
        await Support.EventuallyAsync(() =>
        {
            using IConsumer<string, byte[]> group = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
            {
                BootstrapServers = _container.GetBootstrapAddress(),
                GroupId = KafkaTopology.GroupFor(new EventBusOptions { ExchangeName = exchange, ConsumerClientName = service }, "Pinged"),
            }).Build();
            List<TopicPartitionOffset> committed = group.Committed([new TopicPartition($"{exchange}_Pinged", 0)], TimeSpan.FromSeconds(5));
            return Task.FromResult(committed[0].Offset.Value >= 1);
        }, "the offset of the message to be committed");
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

    [Fact]
    public async Task FetchCount_IsHowManyMessagesOfTheEventAreHandledAtTheSameTime()
    {
        Recorder recorder = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 5);
        });
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();

        for (int number = 0; number < 15; number++)
        {
            await bus.PublishAsync(new Pinged($"message-{number}"));
        }

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 15), "all the messages to be handled");
        Assert.Equal(5, recorder.MaxRunning);
        await host.StopAsync();
    }

    [Fact]
    public async Task WithoutAFetchCount_MessagesAreHandledOneAtATime()
    {
        Recorder recorder = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, SlowHandler>();
        });
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();

        for (int number = 0; number < 4; number++)
        {
            await bus.PublishAsync(new Pinged($"message-{number}"));
        }

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 4), "all the messages to be handled");
        Assert.Equal(1, recorder.MaxRunning);
        await host.StopAsync();
    }

    [Fact]
    public async Task WhatWasHandledAtTheSameTime_IsCommitted_AndIsNotReadAgainByTheNextRun()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        Recorder first = new();
        using (IHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(first);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 5);
        }, "same-service"))
        {
            IEventBus bus = host.Services.GetRequiredService<IEventBus>();
            for (int number = 0; number < 12; number++)
            {
                await bus.PublishAsync(new Pinged($"message-{number}"));
            }

            await Support.EventuallyAsync(() => Task.FromResult(first.Received.Count == 12), "all the messages to be handled");
            await host.StopAsync();
        }

        // the same service again, the same group: the offsets it committed are where it goes on
        Recorder second = new();
        using IHost again = await StartAsync(exchange, services =>
        {
            services.AddSingleton(second);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 5);
        }, "same-service");
        await Task.Delay(TimeSpan.FromSeconds(8));

        Assert.Empty(second.Received);
        await again.StopAsync();
    }

    [Fact]
    public async Task AMessageThatWasCancelledAtShutdown_IsReadAgainByTheNextRun()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        StopProbe probe = new();
        using (IHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(probe);
            services.AddEventHandler<Pinged, LongRunningHandler>(fetchCount: 3);
        }, "same-service", TimeSpan.FromMilliseconds(500)))
        {
            await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("not finished in time"));
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await host.StopAsync();
        }

        Assert.True(probe.Cancelled);

        Recorder second = new();
        using IHost again = await StartAsync(exchange, services =>
        {
            services.AddSingleton(second);
            services.AddEventHandler<Pinged, RecordingHandler>();
        }, "same-service");

        await Support.EventuallyAsync(() => Task.FromResult(second.Received.Count == 1), "the message that was not committed to be read again");
        Assert.Equal("not finished in time", second.Received.Single().Message.Text);
        await again.StopAsync();
    }

    [Fact]
    public async Task AnotherInstanceThatJoinsTheGroup_TakesPartitionsOver_WithoutAMessageBeingHandledTwiceOrLost()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        Recorder onA = new();
        Recorder onB = new();
        using IHost a = await StartAsync(exchange, services =>
        {
            services.AddSingleton(onA);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 3);
        }, "same-service", partitions: 2);
        IEventBus bus = a.Services.GetRequiredService<IEventBus>();
        for (int number = 0; number < 20; number++)
        {
            await bus.PublishAsync(new Pinged($"message-{number}"));
        }

        // the second instance joins while the first one is working: the group is rebalanced, what the first one is handling finishes and is committed before the partition moves
        using IHost b = await StartAsync(exchange, services =>
        {
            services.AddSingleton(onB);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 3);
        }, "same-service", partitions: 2);

        await Support.EventuallyAsync(() => Task.FromResult(onA.Received.Count + onB.Received.Count >= 20), "every message to be handled");
        await Task.Delay(TimeSpan.FromSeconds(5));

        string[] handled = [.. onA.Received.Concat(onB.Received).Select(envelope => envelope.Message.Text)];
        Assert.Equal(20, handled.Length);
        Assert.Equal(20, handled.Distinct().Count());
        await a.StopAsync();
        await b.StopAsync();
    }

    private sealed class InProgressTwiceDispatcher(IEventDispatcher inner) : IEventDispatcher
    {
        public int Asked { get; private set; }

        public Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default)
            => ++Asked <= 2 ? Task.FromException(new HsnSoft.Base.EventBus.Inbox.MessageInProgressException()) : inner.DispatchAsync(trimmedEventName, eventType, messageEnvelope, cancellationToken);
    }

    [Fact]
    public async Task AMessageAnotherHandlingHolds_IsAskedForAgain_UntilItCanBeHandled_AndIsNotDeadLettered()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        Recorder recorder = new();
        InProgressTwiceDispatcher? decorated = null;
        using IHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
            ServiceDescriptor original = services.Single(descriptor => descriptor.ServiceType == typeof(IEventDispatcher));
            services.Remove(original);
            services.AddSingleton<IEventDispatcher>(provider =>
            {
                IEventDispatcher inner = (IEventDispatcher)(original.ImplementationInstance ?? (original.ImplementationFactory is { } factory ? factory(provider) : ActivatorUtilities.CreateInstance(provider, original.ImplementationType!)));
                return decorated = new InProgressTwiceDispatcher(inner);
            });
        }, inProgressRetryDelay: TimeSpan.FromMilliseconds(100));
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("held"));

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 1), "the message to be handled once the other handling let go");
        Assert.Equal("held", recorder.Received.Single().Message.Text);
        Assert.True(decorated!.Asked >= 3);
        await host.StopAsync();
    }

    private sealed class CountingDispatcher : IEventDispatcher
    {
        public int Dispatched { get; private set; }

        public Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default)
        {
            Dispatched++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AMessageThatCannotEvenBeMovedToTheDeadLetterTopic_FailsTheConsumer_IsNotCommitted_AndIsReadAgain()
    {
        string exchange = $"it{Guid.NewGuid():N}";
        KafkaOptions kafka = new() { BootstrapServers = _container.GetBootstrapAddress() };
        EventBusOptions bus = new() { ExchangeName = exchange, ConsumerClientName = "svc", ShutdownTimeout = TimeSpan.FromSeconds(5) };
        IntegrationEventInfo info = new("Pinged", typeof(Pinged), 1);
        string topic = KafkaTopology.TopicFor(bus, "Pinged");
        string deadLetter = KafkaTopology.DeadLetterTopic(bus);
        await KafkaTopology.EnsureTopicsAsync(kafka, [topic, deadLetter]);
        using (IProducer<string, byte[]> producer = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build())
        {
            // not an envelope: the message cannot be read, so it has to go to the dead-letter topic
            await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = "k", Value = "this is not json"u8.ToArray() });
        }

        // a producer that cannot reach any broker: the message cannot be put on the dead-letter topic either
        using IProducer<string, byte[]> unreachable = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = "127.0.0.1:1", MessageTimeoutMs = 500 }).Build();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new KafkaEventConsumer(info, kafka, bus, new CountingDispatcher(), unreachable, deadLetter, NullLogger.Instance).RunAsync(timeout.Token));
        Assert.IsType<ProduceException<string, byte[]>>(failure.InnerException);

        // it was not committed: the same group reads it again, and now the dead-letter topic is there
        using IProducer<string, byte[]> reachable = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        using CancellationTokenSource stop = new();
        Task run = new KafkaEventConsumer(info, kafka, bus, new CountingDispatcher(), reachable, deadLetter, NullLogger.Instance).RunAsync(stop.Token);
        using IConsumer<string, byte[]> probe = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = $"probe-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        probe.Subscribe(deadLetter);
        await Support.EventuallyAsync(() => Task.FromResult(probe.Consume(TimeSpan.FromSeconds(1)) is not null), "the message to be read again and moved to the dead-letter topic");
        await stop.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task AHandlerThatTakesNoNoticeOfItsCancellation_IsNotWaitedForBeyondTheShutdownTimeout()
    {
        StopProbe probe = new();
        using IHost host = await StartAsync($"it{Guid.NewGuid():N}", services =>
        {
            services.AddSingleton(probe);
            services.AddEventHandler<Pinged, StubbornHandler>();
        }, shutdownTimeout: TimeSpan.FromMilliseconds(300));
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("stuck for good"));
        await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        await host.StopAsync();
        watch.Stop();

        // the handler needs six seconds and does not listen: the service stops when the timeout has passed, not when the handler is done
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4), $"the service waited {watch.Elapsed.TotalSeconds:0.0} s for a handler that was stuck");
        Assert.False(probe.Completed);
    }
}
