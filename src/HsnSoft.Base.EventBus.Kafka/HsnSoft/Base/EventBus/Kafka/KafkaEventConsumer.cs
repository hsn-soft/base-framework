using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>
/// One run of the consumer of an event: it reads the topic in the consumer group of its service and event and hands each message to the <see cref="IEventDispatcher"/>, up to <see cref="IntegrationEventInfo.FetchCount"/> messages at the same time (each
/// in a scope of its own, as the dispatcher makes one). A message the dispatcher gives up on is copied to the dead-letter topic. Messages of a partition may finish in any order, so an offset is committed only when every one before it is finished
/// (<see cref="OffsetTracker"/>); a crash, a restart or a rebalance reads again what was not committed. The consumer is used by the one thread that reads: the handlers only say they are done.
/// </summary>
internal sealed partial class KafkaEventConsumer(
    IntegrationEventInfo info,
    KafkaOptions options,
    EventBusOptions eventBusOptions,
    IEventDispatcher dispatcher,
    IProducer<string, byte[]> deadLetters,
    string deadLetterTopic,
    ILogger logger)
{
    private static readonly TimeSpan s_poll = TimeSpan.FromMilliseconds(250);

    private readonly OffsetTracker _tracker = new();
    private readonly Lock _flightLock = new();
    private readonly List<Task> _inFlight = [];
    private Exception? _failure;

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        string topic = KafkaTopology.TopicFor(eventBusOptions, info.EventName);
        string group = KafkaTopology.GroupFor(eventBusOptions, info.EventName);
        int fetchCount = Math.Max((int)info.FetchCount, 1);
        using SemaphoreSlim slots = new(fetchCount, fetchCount);
        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = group,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        })
            .SetPartitionsRevokedHandler((revoking, partitions) => Revoked(revoking, partitions.Select(partition => partition.TopicPartition)))
            .SetPartitionsLostHandler((_, partitions) => _tracker.Forget(partitions.Select(partition => partition.TopicPartition)))
            .Build();

        consumer.Subscribe(topic);
        LogListening(logger, group, topic, fetchCount);

        // a stopping service reads no new message, but the ones it is handling are let finish: their handlers are cancelled only when the shutdown timeout has passed (and the messages are then read again, as they were not committed)
        using CancellationTokenSource handlerCts = new();
        await using CancellationTokenRegistration stopping = stoppingToken.Register(() => handlerCts.CancelAfter(eventBusOptions.ShutdownTimeout));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Commit(consumer);
                ThrowIfAHandlingFailed();

                // every place is taken: look at what finished meanwhile
                if (!await slots.WaitAsync(TimeSpan.FromMilliseconds(100), stoppingToken).ConfigureAwait(false))
                {
                    continue;
                }

                ConsumeResult<string, byte[]>? result;
                try
                {
                    result = consumer.Consume(s_poll);
                }
                catch
                {
                    slots.Release();
                    throw;
                }

                if (result is null)
                {
                    slots.Release();
                    continue;
                }

                _tracker.Register(result.TopicPartition, result.Offset.Value);
                Track(Task.Run(() => HandleAsync(result, slots, handlerCts.Token), CancellationToken.None));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // the host is shutting down
        }
        finally
        {
            await DrainAsync().ConfigureAwait(false);
            try
            {
                Commit(consumer);
            }
            catch (KafkaException exception)
            {
                LogCommitFailed(logger, exception, group);
            }

            consumer.Close();
        }

        ThrowIfAHandlingFailed();
    }

    private async Task HandleAsync(ConsumeResult<string, byte[]> result, SemaphoreSlim slots, CancellationToken handlerToken)
    {
        try
        {
            try
            {
                object envelope = EventSerializer.DeserializeEnvelope(result.Message.Value, info.EventType);
                await dispatcher.DispatchAsync(info.EventName, info.EventType, envelope, handlerToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogDeadLettered(logger, exception, result.Topic);
                await deadLetters.ProduceAsync(deadLetterTopic, new Message<string, byte[]>
                {
                    Key = result.Message.Key,
                    Value = result.Message.Value,
                    Headers = new Headers { { "x-original-topic", Encoding.UTF8.GetBytes(result.Topic) } },
                }, handlerToken).ConfigureAwait(false);
            }

            _tracker.Complete(result.TopicPartition, result.Offset.Value);
        }
        catch (OperationCanceledException)
        {
            // the shutdown timeout has passed: the message is not committed and is read again
        }
        catch (Exception exception)
        {
            // not even the dead-letter topic could be written: the message stays uncommitted, and the consumer starts again
            Interlocked.CompareExchange(ref _failure, exception, null);
        }
        finally
        {
            slots.Release();
        }
    }

    private void Commit(IConsumer<string, byte[]> consumer)
    {
        IReadOnlyList<TopicPartitionOffset> ready = _tracker.TakeReady();
        if (ready.Count > 0)
        {
            consumer.Commit(ready);
        }
    }

    // partitions are taken away (another instance joined the group): what is being handled finishes and is committed first, so the new owner does not read it again
    private void Revoked(IConsumer<string, byte[]> consumer, IEnumerable<TopicPartition> partitions)
    {
        DrainAsync().GetAwaiter().GetResult();
        try
        {
            Commit(consumer);
        }
        catch (KafkaException exception)
        {
            LogCommitFailed(logger, exception, KafkaTopology.GroupFor(eventBusOptions, info.EventName));
        }

        _tracker.Forget(partitions);
    }

    private void Track(Task handling)
    {
        lock (_flightLock)
        {
            _inFlight.RemoveAll(task => task.IsCompleted);
            _inFlight.Add(handling);
        }
    }

    private async Task DrainAsync()
    {
        Task[] running;
        lock (_flightLock)
        {
            running = [.. _inFlight.Where(task => !task.IsCompleted)];
        }

        try
        {
            await Task.WhenAll(running).WaitAsync(eventBusOptions.ShutdownTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // what is still running is not waited for any longer; it is not committed
        }
    }

    private void ThrowIfAHandlingFailed()
    {
        if (Volatile.Read(ref _failure) is { } failure)
        {
            throw new InvalidOperationException("A message could not be dealt with: it was not handled and could not be moved to the dead-letter topic either.", failure);
        }
    }

    [LoggerMessage(EventId = 4501, EventName = "KAFKA_SUBSCRIBER_LISTENING", Level = LogLevel.Information, Message = "{Group} listens to topic {Topic}, {FetchCount} message(s) at a time")]
    private static partial void LogListening(ILogger logger, string group, string topic, int fetchCount);

    [LoggerMessage(EventId = 4502, EventName = "KAFKA_SUBSCRIBER_DEAD_LETTERED", Level = LogLevel.Error, Message = "A message from {Topic} could not be handled, moving it to the dead-letter topic")]
    private static partial void LogDeadLettered(ILogger logger, Exception exception, string topic);

    [LoggerMessage(EventId = 4504, EventName = "KAFKA_SUBSCRIBER_COMMIT_FAILED", Level = LogLevel.Warning, Message = "{Group}: the offsets could not be committed, the messages are read again")]
    private static partial void LogCommitFailed(ILogger logger, Exception exception, string group);
}
