using System.Text;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Inbox;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public sealed class RabbitMqConsumerTests : IAsyncDisposable
{
    private const string ValidMessage = """{"HopLevel":1,"MessageId":"0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07","CorrelationId":"corr","Producer":"p","Message":{"Name":"keyboard","Count":3}}""";

    private readonly List<RabbitMqConsumer> _consumers = [];

    public async ValueTask DisposeAsync()
    {
        foreach (RabbitMqConsumer consumer in _consumers)
        {
            await consumer.DisposeAsync();
        }
    }

    private sealed class Rig
    {
        public required FakeConnection Connection { get; init; }

        public required FakeDispatcher Dispatcher { get; init; }

        public required FakeNotifier Notifier { get; init; }

        public required ListLogger Logger { get; init; }

        public required RabbitMqConsumer Consumer { get; init; }

        public required ChannelState Channel { get; init; }

        public required AsyncEventingBasicConsumer Basic { get; init; }

        public Task Deliver(string json = ValidMessage, ulong tag = 1, string exchange = "HHS", string routingKey = "Sample", string? messageId = null)
            => Basic.HandleBasicDeliverAsync("consumer-tag", tag, false, exchange, routingKey, new BasicProperties { MessageId = messageId }, Encoding.UTF8.GetBytes(json));

        public IReadOnlyList<ChannelCall> Acks => Channel.Named("BasicAckAsync");

        public IReadOnlyList<ChannelCall> Nacks => Channel.Named("BasicNackAsync");
    }

    private async Task<Rig> StartAsync(
        ushort fetchCount = 1,
        Action<EventBusOptions>? configureOptions = null,
        Action<RabbitMqEventBusConfig>? configureConfig = null,
        bool connected = true,
        Type? eventType = null,
        string? eventName = null)
    {
        EventBusOptions options = new() { ExchangeName = "HHS", ConsumerClientName = "OrderService", ShutdownTimeout = TimeSpan.FromSeconds(5) };
        configureOptions?.Invoke(options);
        RabbitMqEventBusConfig config = new() { RequeueDelay = TimeSpan.FromMilliseconds(5), RecoveryProbeInterval = TimeSpan.FromMilliseconds(20) };
        configureConfig?.Invoke(config);

        Type type = eventType ?? typeof(SampleEto);
        InMemoryEventBusSubscriptionManager manager = new(
            [new EventSubscription(type, typeof(SampleHandler), fetchCount)],
            Options.Create(options),
            NullLogger<InMemoryEventBusSubscriptionManager>.Instance);
        IntegrationEventInfo info = manager.GetEventInfoByName(eventName ?? "Sample")!;

        FakeConnection connection = new() { IsConnected = connected };
        ChannelState channel = new();
        connection.QueueChannelState(channel);
        FakeDispatcher dispatcher = new();
        FakeNotifier notifier = new();
        ListLogger logger = new();
        RabbitMqConsumer consumer = new(connection, manager, dispatcher, notifier, options, config, logger, info);
        _consumers.Add(consumer);
        await consumer.StartAsync();

        AsyncEventingBasicConsumer basic = channel.Named("BasicConsumeAsync").Single().Args.OfType<AsyncEventingBasicConsumer>().Single();
        return new Rig { Connection = connection, Dispatcher = dispatcher, Notifier = notifier, Logger = logger, Consumer = consumer, Channel = channel, Basic = basic };
    }

    [Fact]
    public async Task Start_DeclaresTheExchangeAndTheQueue_BindsThem_AndConsumes()
    {
        Rig rig = await StartAsync(fetchCount: 3);

        Assert.Equal("HHS_OrderService_Sample", rig.Consumer.QueueName);
        Assert.Equal(
            ["ExchangeDeclareAsync", "QueueDeclareAsync", "QueueBindAsync", "BasicQosAsync", "BasicConsumeAsync"],
            rig.Channel.Calls.Select(call => call.Name).Where(name => !name.StartsWith("add_", StringComparison.Ordinal)));
        ChannelCall exchange = rig.Channel.Named("ExchangeDeclareAsync").Single();
        Assert.Equal(["HHS", "direct", false], exchange.Args.Take(3));
        ChannelCall queue = rig.Channel.Named("QueueDeclareAsync").Single();
        Assert.Equal(["HHS_OrderService_Sample", true, false, false], queue.Args.Take(4));
        Assert.Null(queue.Args[4]);
        Assert.Equal(["HHS_OrderService_Sample", "HHS", "Sample"], rig.Channel.Named("QueueBindAsync").Single().Args.Take(3));
        Assert.Equal((ushort)3, rig.Channel.Named("BasicQosAsync").Single().Args[1]);
        ChannelCall consume = rig.Channel.Named("BasicConsumeAsync").Single();
        Assert.Equal("HHS_OrderService_Sample", consume.Args[0]);
        Assert.Equal(false, consume.Args[1]);
        Assert.Equal((ushort)3, Assert.Single(rig.Connection.DispatchConcurrencies));
    }

    [Fact]
    public async Task Start_ConnectsFirstWhenNotConnected()
    {
        Rig rig = await StartAsync(connected: false);

        Assert.Equal(1, rig.Connection.ConnectCalls);
    }

    [Fact]
    public async Task HandledMessage_IsDispatchedWithItsEnvelope_AndAcknowledged()
    {
        Rig rig = await StartAsync();

        await rig.Deliver(tag: 7);

        (string eventName, Type eventType, object envelope) = Assert.Single(rig.Dispatcher.Dispatched);
        Assert.Equal("Sample", eventName);
        Assert.Equal(typeof(SampleEto), eventType);
        MessageEnvelope<SampleEto> typed = Assert.IsType<MessageEnvelope<SampleEto>>(envelope);
        Assert.Equal(new SampleEto("keyboard", 3), typed.Message);
        Assert.Equal("corr", typed.CorrelationId);
        ChannelCall ack = Assert.Single(rig.Acks);
        Assert.Equal(7UL, ack.Args[0]);
        Assert.Equal(false, ack.Args[1]);
        Assert.Empty(rig.Nacks);
        Assert.Empty(rig.Notifier.Notified);
    }

    [Fact]
    public async Task MessagePutStraightOnTheQueue_IsRecognizedByTheEndOfItsRoutingKey()
    {
        Rig rig = await StartAsync();

        await rig.Deliver(exchange: string.Empty, routingKey: "HHS_OrderService_Sample");

        Assert.Equal("Sample", Assert.Single(rig.Dispatcher.Dispatched).EventName);
        Assert.Single(rig.Acks);
    }

    [Fact]
    public async Task MessageOfAnEventNobodyHandles_IsAcknowledgedAndWarnedAbout()
    {
        Rig rig = await StartAsync();

        await rig.Deliver(routingKey: "Unknown");

        Assert.Empty(rig.Dispatcher.Dispatched);
        Assert.Single(rig.Acks);
        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("without a subscription for event Unknown", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandlerThatFails_IsReportedToTheErrorClient_AndTheMessageIsTakenOffTheQueue()
    {
        Rig rig = await StartAsync();
        rig.Dispatcher.Behavior = () => throw new InvalidOperationException("handler broke");

        await rig.Deliver(tag: 4);

        (string error, string eventName, string content) = Assert.Single(rig.Notifier.Notified);
        Assert.Equal("handler broke", error);
        Assert.Equal("Sample", eventName);
        Assert.Equal(ValidMessage, content);
        Assert.Equal(4UL, Assert.Single(rig.Acks).Args[0]);
        Assert.Empty(rig.Nacks);
        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task HandlerFailureThatTheHandlingAlreadyLoggedAsAnError_IsNotLoggedAgainHere()
    {
        Rig rig = await StartAsync();
        InvalidOperationException failure = new("handler broke");
        failure.Data[EventDispatcher.ErrorLoggedKey] = true;
        rig.Dispatcher.Behavior = () => throw failure;

        await rig.Deliver(tag: 4);

        Assert.Single(rig.Notifier.Notified);
        // only the consequence is said (the Error is the ConsumeEnd of the handling)
        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("moved to the error queue", StringComparison.Ordinal) && entry.Message.Contains("_Failed", StringComparison.Ordinal));
        Assert.DoesNotContain(rig.Logger.Entries, entry => entry.Level == LogLevel.Error || entry.Message.Contains("ERROR (", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MessageThatCannotBeRead_IsReportedWithItsRawContent()
    {
        Rig rig = await StartAsync();

        await rig.Deliver("this is not json");

        Assert.Empty(rig.Dispatcher.Dispatched);
        Assert.Equal("this is not json", Assert.Single(rig.Notifier.Notified).Content);
        Assert.Single(rig.Acks);
    }

    [Fact]
    public async Task WhatTheConsumerLogs_BelongsToTheChainOfTheMessage()
    {
        Rig rig = await StartAsync();

        await rig.Deliver();
        await rig.Deliver("not json at all", tag: 2);

        IReadOnlyDictionary<string, object?> readable = rig.Logger.Scopes[0];
        IReadOnlyDictionary<string, object?> unreadable = rig.Logger.Scopes[1];
        Assert.Equal("corr", readable["CorrelationId"]);
        Assert.Equal(Guid.Parse("0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07"), readable["MessageId"]);
        Assert.Equal("Sample", readable["EventName"]);
        Assert.Equal("Sample", unreadable["EventName"]);
        Assert.False(unreadable.ContainsKey("CorrelationId"));
    }

    [Fact]
    public async Task ReceivedMessage_IsLoggedWithoutItsBody_ByDefault()
    {
        Rig rig = await StartAsync();

        await rig.Deliver();

        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("received Sample", StringComparison.Ordinal));
        Assert.DoesNotContain(rig.Logger.Entries, entry => entry.Message.Contains("keyboard", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReceivedMessage_IsLoggedWithItsBody_WhenLogMessageBodyIsOn()
    {
        Rig rig = await StartAsync(configureOptions: options => options.LogMessageBody = true);

        await rig.Deliver();

        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("keyboard", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DroppedFailedEvent_IsLoggedWithoutItsBody_ByDefault_AndWithItWhenLogMessageBodyIsOn()
    {
        const string Body = """{"HopLevel":1,"Message":{"FailedReason":"secret-reason"}}""";

        Rig quiet = await StartAsync(eventType: typeof(FailedEto), eventName: "Failed");
        quiet.Dispatcher.Behavior = () => throw new InvalidOperationException("broke");
        await quiet.Deliver(Body, routingKey: "Failed");
        Assert.DoesNotContain(quiet.Logger.Entries, entry => entry.Message.Contains("secret-reason", StringComparison.Ordinal));
        Assert.Contains(quiet.Logger.Entries, entry => entry.Level == LogLevel.Critical && entry.Message.Contains("body not logged", StringComparison.Ordinal));

        Rig verbose = await StartAsync(configureOptions: options => options.LogMessageBody = true, eventType: typeof(FailedEto), eventName: "Failed");
        verbose.Dispatcher.Behavior = () => throw new InvalidOperationException("broke");
        await verbose.Deliver(Body, routingKey: "Failed");
        Assert.Contains(verbose.Logger.Entries, entry => entry.Level == LogLevel.Critical && entry.Message.Contains("secret-reason", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailedEventThatFails_IsNotReportedAgain()
    {
        Rig rig = await StartAsync(eventType: typeof(FailedEto), eventName: "Failed");
        rig.Dispatcher.Behavior = () => throw new InvalidOperationException("the error handler broke");

        await rig.Deliver("""{"HopLevel":1,"Message":{"FailedReason":"x"}}""", routingKey: "Failed");

        Assert.Empty(rig.Notifier.Notified);
        Assert.Single(rig.Acks);
        Assert.Contains(rig.Logger.Entries, entry => entry.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task WhenTheReportItselfFails_TheMessageGoesBackOnTheQueue()
    {
        Rig rig = await StartAsync();
        rig.Dispatcher.Behavior = () => throw new InvalidOperationException("handler broke");
        rig.Notifier.Failure = new InvalidOperationException("broker down");

        await rig.Deliver(tag: 9);

        Assert.Empty(rig.Acks);
        ChannelCall nack = Assert.Single(rig.Nacks);
        Assert.Equal([9UL, false, true], nack.Args.Take(3));
    }

    [Fact]
    public async Task Timeout_PutsTheMessageBackOnTheQueue_WithoutAReport()
    {
        Rig rig = await StartAsync();
        rig.Dispatcher.Behavior = () => throw new TimeoutException("too slow");

        await rig.Deliver(tag: 3);

        Assert.Empty(rig.Notifier.Notified);
        Assert.Empty(rig.Acks);
        Assert.Equal([3UL, false, true], Assert.Single(rig.Nacks).Args.Take(3));
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("TIMEOUT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AMessageWhoseHandlerKeepsTimingOut_CountsAsFailedAfterTheLimit()
    {
        Rig rig = await StartAsync(configureConfig: config => config.MaxTimeoutRequeues = 2);
        rig.Dispatcher.Behavior = () => throw new TimeoutException("too slow");

        await rig.Deliver(tag: 1, messageId: "m-1");
        await rig.Deliver(tag: 2, messageId: "m-1");
        Assert.Empty(rig.Notifier.Notified);
        Assert.Equal(2, rig.Nacks.Count);

        await rig.Deliver(tag: 3, messageId: "m-1");

        Assert.Single(rig.Notifier.Notified);
        Assert.Equal(3UL, Assert.Single(rig.Acks).Args[0]);
        Assert.Equal(2, rig.Nacks.Count);
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("timed out more than 2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ATimeoutCount_IsPerMessage_AndIsForgottenWhenTheMessageIsHandled()
    {
        Rig rig = await StartAsync(configureConfig: config => config.MaxTimeoutRequeues = 1);
        bool fail = true;
        rig.Dispatcher.Behavior = () => fail ? throw new TimeoutException("too slow") : Task.CompletedTask;

        await rig.Deliver(tag: 1, messageId: "m-1");
        await rig.Deliver(tag: 2, messageId: "m-2");
        fail = false;
        await rig.Deliver(tag: 3, messageId: "m-1");
        fail = true;
        await rig.Deliver(tag: 4, messageId: "m-1");

        // each message timed out once (the limit), m-1 was handled in between and started again: nothing is reported
        Assert.Empty(rig.Notifier.Notified);
        Assert.Equal([1UL, 2UL, 4UL], rig.Nacks.Select(nack => (ulong)nack.Args[0]!));
    }

    [Fact]
    public async Task ATimeoutWithoutAMessageId_IsPutBackWithoutALimit()
    {
        Rig rig = await StartAsync(configureConfig: config => config.MaxTimeoutRequeues = 1);
        rig.Dispatcher.Behavior = () => throw new TimeoutException("too slow");

        await rig.Deliver(tag: 1);
        await rig.Deliver(tag: 2);
        await rig.Deliver(tag: 3);

        Assert.Empty(rig.Notifier.Notified);
        Assert.Equal(3, rig.Nacks.Count);
    }

    [Fact]
    public async Task AMessageAnotherHandlingHolds_IsPutBackOnTheQueue_NotAcknowledgedAndNotReported()
    {
        Rig rig = await StartAsync();
        rig.Dispatcher.Behavior = () => throw new MessageInProgressException();

        await rig.Deliver(tag: 4);

        Assert.Empty(rig.Notifier.Notified);
        Assert.Empty(rig.Acks);
        Assert.Equal([4UL, false, true], Assert.Single(rig.Nacks).Args.Take(3));
    }

    [Fact]
    public async Task Timeout_OnAChannelThatIsGone_IsLogged()
    {
        // the connection is down, so the recovery worker leaves the closed channel alone whenever it looks at it
        Rig rig = await StartAsync();
        rig.Connection.IsConnected = false;
        rig.Dispatcher.Behavior = () =>
        {
            rig.Channel.IsClosed = true;
            throw new TimeoutException("too slow");
        };

        await rig.Deliver();

        Assert.Empty(rig.Nacks);
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be put back on the queue", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandledMessage_WhoseAcknowledgementFails_IsNotReportedAsFailed()
    {
        Rig rig = await StartAsync();
        rig.Dispatcher.Behavior = () =>
        {
            rig.Channel.IsClosed = true;
            return Task.CompletedTask;
        };

        await rig.Deliver();

        Assert.Empty(rig.Notifier.Notified);
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be acknowledged", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FetchCount_LimitsHowManyMessagesAreHandledAtTheSameTime()
    {
        Rig rig = await StartAsync(fetchCount: 2);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Dispatcher.Behavior = () => release.Task;

        Task[] deliveries = [rig.Deliver(tag: 1), rig.Deliver(tag: 2), rig.Deliver(tag: 3)];
        await EventBusRabbitMqTests.Until(() => rig.Dispatcher.Running == 2);
        await Task.Delay(100);
        Assert.Equal(2, rig.Dispatcher.Running);

        release.SetResult();
        await Task.WhenAll(deliveries);

        Assert.Equal(2, rig.Dispatcher.MaxRunning);
        Assert.Equal(3, rig.Dispatcher.Dispatched.Count);
        Assert.Equal(3, rig.Acks.Count);
    }

    [Fact]
    public async Task Dispose_WaitsForTheMessagesInFlight_ThenClosesTheChannel()
    {
        Rig rig = await StartAsync();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Dispatcher.Behavior = () => release.Task;
        Task delivery = rig.Deliver(tag: 5);
        await EventBusRabbitMqTests.Until(() => rig.Dispatcher.Running == 1);

        Task disposing = rig.Consumer.DisposeAsync().AsTask();
        await Task.Delay(300);
        Assert.False(disposing.IsCompleted);
        Assert.Empty(rig.Channel.Named("CloseAsync"));

        release.SetResult();
        await delivery;
        await disposing;

        Assert.Single(rig.Acks);
        Assert.Single(rig.Channel.Named("CloseAsync"));
        string[] order = [.. rig.Channel.Calls.Select(call => call.Name).Where(name => name is "BasicAckAsync" or "CloseAsync")];
        Assert.Equal(["BasicAckAsync", "CloseAsync"], order);
    }

    [Fact]
    public async Task Dispose_StopsTheBrokerFromDelivering_BeforeWaitingForTheMessagesInFlight()
    {
        Rig rig = await StartAsync();

        await rig.Consumer.DisposeAsync();

        string[] order = [.. rig.Channel.Calls.Select(call => call.Name).Where(name => name is "BasicCancelAsync" or "CloseAsync")];
        Assert.Equal(["BasicCancelAsync", "CloseAsync"], order);
        Assert.Equal("consumer-tag", rig.Channel.Named("BasicCancelAsync").Single().Args[0]);
    }

    [Fact]
    public async Task Dispose_ClosesTheChannelEvenWhenTheConsumerCannotBeCancelled()
    {
        Rig rig = await StartAsync();
        rig.Channel.Failure = call => call.Name == "BasicCancelAsync" ? new InvalidOperationException("channel is gone") : null;

        await rig.Consumer.DisposeAsync();

        Assert.Single(rig.Channel.Named("CloseAsync"));
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dispose_OfAChannelThatIsAlreadyClosed_DoesNotTryToCancel()
    {
        Rig rig = await StartAsync();
        rig.Channel.IsClosed = true;

        await rig.Consumer.DisposeAsync();

        Assert.Empty(rig.Channel.Named("BasicCancelAsync"));
    }

    [Fact]
    public async Task HandlerThatRunsOutOfTime_IsCancelled_AndItsMessageStaysUnacknowledged()
    {
        Rig rig = await StartAsync(configureOptions: options => options.ShutdownTimeout = TimeSpan.FromMilliseconds(150));
        rig.Dispatcher.Behavior = async () => await Task.Delay(Timeout.Infinite, rig.Dispatcher.LastToken);
        Task delivery = rig.Deliver();
        await EventBusRabbitMqTests.Until(() => rig.Dispatcher.Running == 1);

        await rig.Consumer.DisposeAsync();
        await delivery;

        Assert.Empty(rig.Acks);
        Assert.Empty(rig.Nacks);
        Assert.Empty(rig.Notifier.Notified);
    }

    [Fact]
    public async Task MessageThatArrivesAfterDispose_IsLeftAlone()
    {
        Rig rig = await StartAsync();
        await rig.Consumer.DisposeAsync();

        await rig.Deliver();

        Assert.Empty(rig.Dispatcher.Dispatched);
        Assert.Empty(rig.Acks);
    }

    [Fact]
    public async Task Dispose_IsRepeatable_AndSurvivesAChannelThatCannotBeClosed()
    {
        Rig rig = await StartAsync();
        rig.Channel.Failure = call => call.Name == "CloseAsync" ? new InvalidOperationException("already gone") : null;

        await rig.Consumer.DisposeAsync();
        await rig.Consumer.DisposeAsync();

        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be closed cleanly", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChannelThatDiesOnItsOwn_IsRebuilt()
    {
        Rig rig = await StartAsync();

        rig.Channel.IsClosed = true;
        await EventBusRabbitMqTests.Until(() => rig.Connection.Channels.Count == 2);

        ChannelState rebuilt = rig.Connection.Channels[1];
        await EventBusRabbitMqTests.Until(() => rebuilt.Named("BasicConsumeAsync").Count == 1);
        Assert.Equal(["HHS", "direct"], rebuilt.Named("ExchangeDeclareAsync").Single().Args.Take(2));
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("died and was rebuilt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsumerTheBrokerCancels_OnAnOpenChannel_IsRebuilt()
    {
        Rig rig = await StartAsync();

        // the queue was deleted or a node failed over: the channel stays open but nothing is delivered to it any more
        await rig.Basic.HandleBasicCancelAsync("consumer-tag");
        await EventBusRabbitMqTests.Until(() => rig.Connection.Channels.Count == 2 && rig.Connection.Channels[1].Named("BasicConsumeAsync").Count == 1);

        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("broker cancelled the consumer", StringComparison.Ordinal));
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("died and was rebuilt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RebuildThatFailsAfterTheChannelWasCreated_ThrowsTheChannelAway_AndIsTriedAgain()
    {
        Rig rig = await StartAsync();
        ChannelState failing = new() { Failure = call => call.Name == "BasicConsumeAsync" ? new InvalidOperationException("consume failed") : null };
        rig.Connection.QueueChannelState(failing);

        rig.Channel.IsClosed = true;

        // the channel without a consumer is not kept as if it were alive: a third channel is made and consumed on
        await EventBusRabbitMqTests.Until(() => rig.Connection.Channels.Count == 3 && rig.Connection.Channels[2].Named("BasicConsumeAsync").Count == 1);
        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be recovered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RebuiltChannel_ReceivesMessages()
    {
        Rig rig = await StartAsync();
        rig.Channel.IsClosed = true;
        await EventBusRabbitMqTests.Until(() => rig.Connection.Channels.Count == 2 && rig.Connection.Channels[1].Named("BasicConsumeAsync").Count == 1);
        ChannelState rebuilt = rig.Connection.Channels[1];
        AsyncEventingBasicConsumer basic = rebuilt.Named("BasicConsumeAsync").Single().Args.OfType<AsyncEventingBasicConsumer>().Single();

        await basic.HandleBasicDeliverAsync("tag", 11, false, "HHS", "Sample", new BasicProperties(), Encoding.UTF8.GetBytes(ValidMessage));

        Assert.Equal(11UL, Assert.Single(rebuilt.Named("BasicAckAsync")).Args[0]);
    }

    [Fact]
    public async Task ChannelIsLeftAlone_WhileTheConnectionRecovers()
    {
        Rig rig = await StartAsync();
        rig.Connection.IsConnected = false;

        // let the recovery worker see the lost connection first: it reads the connection and the channel one after the other
        await Task.Delay(100);
        rig.Channel.IsClosed = true;
        await Task.Delay(200);

        Assert.Single(rig.Connection.Channels);
    }

    [Fact]
    public async Task FailedRebuild_IsLogged_AndTriedAgain()
    {
        Rig rig = await StartAsync();
        rig.Connection.ChannelsToFail = 1;

        rig.Channel.IsClosed = true;
        await EventBusRabbitMqTests.Until(() => rig.Connection.Channels.Count == 2);

        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("could not be recovered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChannelShutdown_IsLogged()
    {
        Rig rig = await StartAsync();
        AsyncEventHandler<ShutdownEventArgs> handler = rig.Channel.Named("add_ChannelShutdownAsync").Single().Args.OfType<AsyncEventHandler<ShutdownEventArgs>>().Single();

        await handler(rig, new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "NOT_FOUND"));

        Assert.Contains(rig.Logger.Entries, entry => entry.Message.Contains("shut down (NOT_FOUND)", StringComparison.Ordinal));
    }
}
