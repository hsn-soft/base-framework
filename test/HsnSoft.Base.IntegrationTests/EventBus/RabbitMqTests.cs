using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Containers;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;

namespace HsnSoft.Base.IntegrationTests.EventBus;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    public RabbitMqContainer Container { get; } = new RabbitMqBuilder("rabbitmq:4").WithUsername("guest").WithPassword("guest").Build();

    public string Host => Container.Hostname;

    public ushort Port => Container.GetMappedPublicPort(5672);

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Support.DisposeAsync(Container);

    public async Task<IConnection> RawConnectionAsync()
        => await new ConnectionFactory { HostName = Host, Port = Port, UserName = "guest", Password = "guest" }.CreateConnectionAsync();
}

/// <summary>A logger provider whose first scope of the consumer fails: a step of the framework that fails before the handler is called.</summary>
public sealed class FailsOnceBeforeTheHandler : ILoggerProvider
{
    private int _failed;

    public ILogger CreateLogger(string categoryName) => new FailingLogger(this, categoryName.EndsWith("RabbitMqConsumer", StringComparison.Ordinal));

    public void Dispose()
    {
    }

    private sealed class FailingLogger(FailsOnceBeforeTheHandler owner, bool consumer) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => consumer && Interlocked.Exchange(ref owner._failed, 1) == 0 ? throw new InvalidOperationException("a step before the handler failed") : null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}

public sealed class UserHeaders : IEventHeaderProvider
{
    public string? GetCorrelationId() => null;

    public void AddHeaders(IDictionary<string, string> headers) => headers["TraceUser"] = "user-9";
}

/// <summary>
/// The RabbitMQ event bus against a real broker. What the services in production already do on the broker is the reference:
/// the names of the exchange and the queues, the parameters they are declared with, and the JSON on the wire.
/// </summary>
public sealed class RabbitMqTests(RabbitMqFixture fixture) : IClassFixture<RabbitMqFixture>
{
    private static string NewExchange() => $"it{Guid.NewGuid():N}";

    private IConfiguration Configuration(string exchange, string? host = null, string? port = null)
        => Support.Configuration(
            ("RabbitMq:Connection:HostName", host ?? fixture.Host),
            ("RabbitMq:Connection:Port", port ?? fixture.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("RabbitMq:Connection:ConnectionRetryCount", "1"),
            ("RabbitMq:Connection:ConnectionRetryBaseDelay", "00:00:00.1"),
            ("RabbitMq:EventBus:ExchangeName", exchange),
            ("RabbitMq:EventBus:ConsumerClientName", "it-service"),
            ("RabbitMq:EventBus:ErrorClientName", "it-error"),
            ("RabbitMq:EventBus:RequeueDelay", "00:00:00.1"),
            ("RabbitMq:EventBus:RecoveryProbeInterval", "00:00:00.2"));

    private async Task<RunningHost> StartAsync(string exchange, Action<IServiceCollection> handlers, IConfiguration? configuration = null, Action<EventBusOptions>? configureOptions = null)
        => new(await Support.StartHostAsync(services =>
        {
            services.AddRabbitMqEventBus(configuration ?? Configuration(exchange), configureEventBus: options =>
            {
                options.PublishRetryBaseDelay = TimeSpan.FromMilliseconds(100);
                configureOptions?.Invoke(options);
            });
            handlers(services);
        }));

    /// <summary>Stops the host before it is disposed, as a real host does: the consumers finish what they are handling first.</summary>
    private sealed class RunningHost(IHost host) : IAsyncDisposable
    {
        public IServiceProvider Services => host.Services;

        public Task StopAsync() => host.StopAsync();

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static Task<bool> Has<T>(System.Collections.Concurrent.ConcurrentQueue<T> queue) => Task.FromResult(!queue.IsEmpty);

    private static async Task<BasicGetResult> GetAsync(IChannel channel, string queue)
    {
        BasicGetResult? message = null;
        await Support.EventuallyAsync(
            async () =>
            {
                message = await channel.BasicGetAsync(queue, autoAck: true);
                return message is not null;
            },
            $"a message in {queue}");
        return message!;
    }

    private static string Text(BasicGetResult message) => Encoding.UTF8.GetString(message.Body.Span);

    [Fact]
    public async Task PublishedEvent_ReachesTheHandler_WithItsEnvelope_AndTheServiceIsReady()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("hello"), correlationId: "corr-1");

        await Support.EventuallyAsync(() => Has(recorder.Received), "the handler to receive the event");
        recorder.Received.TryPeek(out MessageEnvelope<Pinged>? envelope);
        Assert.Equal("hello", envelope!.Message.Text);
        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.Equal((ushort)1, envelope.HopLevel);
        Assert.Equal("it-service", envelope.Producer);
        Assert.Null(envelope.ParentMessageId);
        Assert.NotEqual(Guid.Empty, envelope.MessageId);
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }

    [Fact]
    public async Task AnEventNobodyListensTo_IsNotAnError_AndAPublishThatIsConfirmedStillReaches()
    {
        string exchange = NewExchange();
        await using RunningHost publisher = await StartAsync(exchange, _ => { });
        IEventBus bus = publisher.Services.GetRequiredService<IEventBus>();

        // no queue is bound yet: the broker returns the message, which is told in the log but is not a failure of the publisher
        await bus.PublishAsync(new Pinged("nobody"));

        Recorder recorder = new();
        await using RunningHost subscriber = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        await bus.PublishAsync(new Pinged("somebody"));

        await Support.EventuallyAsync(() => Has(recorder.Received), "the event to reach the queue that is bound now");
        recorder.Received.TryPeek(out MessageEnvelope<Pinged>? envelope);
        Assert.Equal("somebody", envelope!.Message.Text);
    }

    [Fact]
    public async Task ExchangeAndQueue_AreDeclaredLikeTheProductionServicesDeclareThem()
    {
        string exchange = NewExchange();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(new Recorder());
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        string queue = $"{exchange}_it-service_Pinged";

        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        // declaring again with the parameters the production code uses fails with PRECONDITION_FAILED if ours differ
        await channel.ExchangeDeclareAsync(exchange, "direct");
        QueueDeclareOk declared = await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        Assert.Equal(queue, declared.QueueName);
        Assert.Equal(1u, declared.ConsumerCount);
    }

    [Fact]
    public async Task MessageWrittenByTheProductionFramework_IsHandled()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        string json = $$$"""
            {"HopLevel":3,"ReQueuedCount":0,"MessageId":"{{{Guid.NewGuid()}}}","MessageTime":"2026-05-01T08:30:00Z","CorrelationId":"prod-corr",
             "TraceUser":"user-7","Region":null,"ClientLat":null,"ClientChannel":"web","Producer":"HHS_Other","ParentMessageId":null,
             "Message":{"Text":"from production"}}
            """;

        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        await channel.BasicPublishAsync(exchange, "Pinged", mandatory: true, new BasicProperties { DeliveryMode = DeliveryModes.Persistent }, Encoding.UTF8.GetBytes(json));

        await Support.EventuallyAsync(() => Has(recorder.Received), "the handler to receive the production message");
        recorder.Received.TryPeek(out MessageEnvelope<Pinged>? envelope);
        Assert.Equal("from production", envelope!.Message.Text);
        Assert.Equal("prod-corr", envelope.CorrelationId);
        Assert.Equal((ushort)3, envelope.HopLevel);
        Assert.Equal("HHS_Other", envelope.Producer);
        Assert.Equal("user-7", envelope.GetHeader("TraceUser"));
        Assert.Equal("web", envelope.GetHeader("ClientChannel"));
    }

    [Fact]
    public async Task MessageWrittenByTheFramework_HasTheShapeTheProductionServicesRead()
    {
        string exchange = NewExchange();
        await using RunningHost host = await StartAsync(
            exchange,
            services =>
            {
                services.AddSingleton(new Recorder());
                services.AddEventHandler<Pinged, RecordingHandler>();
                services.AddEventHeaderProvider<UserHeaders>();
            });
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        QueueDeclareOk probe = await channel.QueueDeclareAsync(string.Empty, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(probe.QueueName, exchange, "Pinged");

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("shape"), correlationId: "corr-shape");

        BasicGetResult message = await GetAsync(channel, probe.QueueName);
        using JsonDocument document = JsonDocument.Parse(Text(message));
        JsonElement root = document.RootElement;
        // Flow is the one property the framework added to the production shape (where the message was published from, for the logs); readers ignore what they do not know
        string[] expected = ["CorrelationId", "Flow", "HopLevel", "Message", "MessageId", "MessageTime", "ParentMessageId", "Producer", "ReQueuedCount", "TraceUser"];
        string[] actual = [.. root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
        Assert.Equal(expected, actual);
        Assert.Equal("corr-shape", root.GetProperty("CorrelationId").GetString());
        Assert.Equal(1, root.GetProperty("HopLevel").GetInt32());
        Assert.Equal("it-service", root.GetProperty("Producer").GetString());
        Assert.Equal("user-9", root.GetProperty("TraceUser").GetString());
        Assert.Equal("agent", root.GetProperty("Flow").GetString());
        Assert.Equal("shape", root.GetProperty("Message").GetProperty("Text").GetString());
        Assert.Equal(DeliveryModes.Persistent, message.BasicProperties.DeliveryMode);
    }

    [Fact]
    public async Task EventPublishedByAHandler_ContinuesTheChainWithoutBeingTold()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, ChainingHandler>();
            services.AddEventHandler<Ponged, FollowUpHandler>();
            services.AddEventHeaderProvider<UserHeaders>();
        });

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("chain"), correlationId: "chain-corr");

        await Support.EventuallyAsync(() => Has(recorder.Followed), "the follow-up event");
        recorder.Received.TryPeek(out MessageEnvelope<Pinged>? parent);
        recorder.Followed.TryPeek(out MessageEnvelope<Ponged>? child);
        Assert.Equal(parent!.MessageId, child!.ParentMessageId);
        Assert.Equal((ushort)(parent.HopLevel + 1), child.HopLevel);
        Assert.Equal("chain-corr", child.CorrelationId);
        Assert.Equal("user-9", child.GetHeader("TraceUser"));
        Assert.Equal("chain", child.Message.Text);
    }

    [Fact]
    public async Task AStepOfTheFrameworkThatFailsBeforeTheHandler_ReportsTheMessage_AndTheConsumerGoesOn()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddLogging(logging => logging.AddProvider(new FailsOnceBeforeTheHandler()));
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync($"{exchange}_it-error_Failed", durable: true, exclusive: false, autoDelete: false, arguments: null);

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("first"));
        BasicGetResult failure = await GetAsync(channel, $"{exchange}_it-error_Failed");
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("second"));

        MessageEnvelope<FailedEto> envelope = (MessageEnvelope<FailedEto>)EventSerializer.DeserializeEnvelope(failure.Body.Span, typeof(FailedEto));
        Assert.Contains("a step before the handler failed", envelope.Message.FailedReason, StringComparison.Ordinal);
        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 1), "the next message to be handled");
        Assert.Equal("second", recorder.Received.Single().Message.Text);
        QueueDeclareOk work = await channel.QueueDeclarePassiveAsync($"{exchange}_it-service_Pinged");
        Assert.Equal(0u, work.MessageCount);
    }

    [Fact]
    public async Task HandlerThatFails_ReportsToTheErrorClientsQueue_AndTheMessageLeavesItsQueue()
    {
        string exchange = NewExchange();
        await using RunningHost host = await StartAsync(exchange, services => services.AddEventHandler<Pinged, FailingHandler>());
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        // declared the way the production services declare it; the framework then declares it again with the same parameters
        await channel.QueueDeclareAsync($"{exchange}_it-error_Failed", durable: true, exclusive: false, autoDelete: false, arguments: null);

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("doomed"), correlationId: "fail-corr");

        BasicGetResult failure = await GetAsync(channel, $"{exchange}_it-error_Failed");
        MessageEnvelope<FailedEto> envelope = (MessageEnvelope<FailedEto>)EventSerializer.DeserializeEnvelope(failure.Body.Span, typeof(FailedEto));
        Assert.Contains("this handler always fails", envelope.Message.FailedReason, StringComparison.Ordinal);
        Assert.Equal("Pinged", envelope.Message.FailedMessageTypeName);
        Assert.Equal("doomed", ((JsonElement)envelope.Message.FailedMessageObject!).GetProperty("Text").GetString());
        Assert.NotNull(envelope.ParentMessageId);
        Assert.Equal((ushort)2, envelope.HopLevel);
        Assert.Equal("fail-corr", envelope.CorrelationId);
        Assert.Equal("it-service", envelope.Producer);

        QueueDeclareOk work = await channel.QueueDeclarePassiveAsync($"{exchange}_it-service_Pinged");
        Assert.Equal(0u, work.MessageCount);
    }

    [Fact]
    public async Task ReQueuedMessage_IsPutStraightOnTheQueueOfTheConsumer()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<ReQueuedEto, ReQueuedHandler>();
        });

        await host.Services.GetRequiredService<IEventBus>().SendAsync(
            new ReQueuedEto($"{exchange}_it-service", new Pinged("again"), "Pinged"),
            "it-service",
            isReQueuePublish: true);

        await Support.EventuallyAsync(() => Has(recorder.ReQueued), "the requeued message");
        recorder.ReQueued.TryPeek(out MessageEnvelope<ReQueuedEto>? envelope);
        Assert.Equal((ushort)1, envelope!.ReQueuedCount);
        Assert.Equal($"{exchange}_it-service", envelope.Message.ReQueuedMessageEnvelopeConsumer);
        Assert.Equal("Pinged", envelope.Message.ReQueuedMessageTypeName);
        Assert.Equal("again", ((JsonElement)envelope.Message.ReQueuedMessageObject).GetProperty("Text").GetString());
    }

    [Fact]
    public async Task FetchCount_SetsHowManyMessagesAreHandledAtTheSameTime()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, SlowHandler>(fetchCount: 3);
        });
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();

        for (int index = 0; index < 6; index++)
        {
            await bus.PublishAsync(new Pinged($"message-{index}"));
        }

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 6), "all six messages to be handled");
        Assert.Equal(3, recorder.MaxRunning);
    }

    [Fact]
    public async Task Timeout_PutsTheMessageBackOnTheQueue_AndItIsHandledNextTime()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, TimeoutOnceHandler>();
        });

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("slow dependency"));

        await Support.EventuallyAsync(() => Has(recorder.Received), "the message to be handled the second time");
        Assert.Equal(2, recorder.Calls);
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        QueueDeclareOk errors = await channel.QueueDeclareAsync($"{exchange}_it-error_Failed", durable: true, exclusive: false, autoDelete: false, arguments: null);
        Assert.Equal(0u, errors.MessageCount);
    }

    [Fact]
    public async Task Stop_WaitsForTheMessageBeingHandled_ThenAcknowledgesIt()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, SlowHandler>();
        });
        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("in flight"));
        await Support.EventuallyAsync(() => Task.FromResult(recorder.Running == 1), "the handler to start");

        await host.StopAsync();

        Assert.Single(recorder.Received);
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        QueueDeclareOk work = await channel.QueueDeclarePassiveAsync($"{exchange}_it-service_Pinged");
        Assert.Equal(0u, work.MessageCount);
        Assert.Equal(0u, work.ConsumerCount);
    }

    [Fact]
    public async Task APublishDuringTheRecovery_WaitsForTheClientToRecover_AndTheConsumerStaysOnItsConnection()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        }, configureOptions: options => options.PublishRetryCount = 8);
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();
        await bus.PublishAsync(new Pinged("before"));
        await Support.EventuallyAsync(() => Has(recorder.Received), "the first message");

        await fixture.Container.ExecAsync(["rabbitmqctl", "close_all_connections", "recovery test"]);

        // one publish, no loop of the caller: it is repeated until the client has recovered the connection, and it does not replace the connection the client is recovering
        await bus.PublishAsync(new Pinged("during"));

        await Support.EventuallyAsync(() => Task.FromResult(recorder.Received.Count == 2), "the message published during the recovery to reach the consumer that was recovered");
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Equal(2, recorder.Received.Count);
        ExecResult connections = await fixture.Container.ExecAsync(["rabbitmqctl", "list_connections", "name"]);
        Assert.Equal(1, connections.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(line => line.Contains("->", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ConnectionThatIsCutOff_IsRecovered_AndMessagesFlowAgain()
    {
        string exchange = NewExchange();
        Recorder recorder = new();
        await using RunningHost host = await StartAsync(exchange, services =>
        {
            services.AddSingleton(recorder);
            services.AddEventHandler<Pinged, RecordingHandler>();
        });
        IEventBus bus = host.Services.GetRequiredService<IEventBus>();
        await bus.PublishAsync(new Pinged("before"));
        await Support.EventuallyAsync(() => Has(recorder.Received), "the first message");

        await fixture.Container.ExecAsync(["rabbitmqctl", "close_all_connections", "recovery test"]);

        await Support.EventuallyAsync(
            async () =>
            {
                try
                {
                    await bus.PublishAsync(new Pinged("after"));
                }
                catch (Exception exception) when (exception is AlreadyClosedException or BrokerUnreachableException or InvalidOperationException)
                {
                    // the connection is being re-established
                }

                await Task.Delay(500);
                return recorder.Received.Any(envelope => envelope.Message.Text == "after");
            },
            "a message to be handled after the connection was cut off");
    }

    [Fact]
    public async Task ServiceThatOnlyPublishes_ConnectsOnDemand_AndIsReady()
    {
        string exchange = NewExchange();
        await using RunningHost host = await StartAsync(exchange, _ => { });
        await using IConnection connection = await fixture.RawConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange, "direct");
        QueueDeclareOk probe = await channel.QueueDeclareAsync(string.Empty, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(probe.QueueName, exchange, "Pinged");
        IRabbitMqPersistentConnection persistent = host.Services.GetRequiredService<IRabbitMqPersistentConnection>();
        Assert.False(persistent.IsConnected);

        await host.Services.GetRequiredService<IEventBus>().PublishAsync(new Pinged("only publishing"));
        await host.Services.GetRequiredService<IEventBus>().SendAsync(new Pinged("own queue"), "it-service");

        Assert.True(persistent.IsConnected);
        Assert.Contains("only publishing", Text(await GetAsync(channel, probe.QueueName)), StringComparison.Ordinal);
        QueueDeclareOk own = await channel.QueueDeclarePassiveAsync($"{exchange}_it-service_Pinged");
        Assert.Equal(1u, own.MessageCount);
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }

    [Fact]
    public async Task BrokerThatCannotBeReached_FailsTheStartOfTheService()
    {
        string exchange = NewExchange();

        await Assert.ThrowsAsync<BrokerUnreachableException>(() => StartAsync(
            exchange,
            services => services.AddEventHandler<Pinged, FailingHandler>(),
            Configuration(exchange, host: "127.0.0.1", port: "1")));
    }
}
