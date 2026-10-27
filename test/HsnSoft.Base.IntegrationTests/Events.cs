using System.Collections.Concurrent;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.IntegrationTests;

public sealed record Pinged(string Text) : IIntegrationEventMessage;

public sealed record Ponged(string Text) : IIntegrationEventMessage;

public sealed class Recorder
{
    public ConcurrentQueue<MessageEnvelope<Pinged>> Received { get; } = new();

    public ConcurrentQueue<MessageEnvelope<Ponged>> Followed { get; } = new();

    public ConcurrentQueue<MessageEnvelope<ReQueuedEto>> ReQueued { get; } = new();

    public int Running { get; private set; }

    public int MaxRunning { get; private set; }

    public int Calls { get; private set; }

    internal void Enter()
    {
        lock (this)
        {
            Calls++;
            Running++;
            MaxRunning = Math.Max(MaxRunning, Running);
        }
    }

    internal void Leave()
    {
        lock (this)
        {
            Running--;
        }
    }
}

public sealed class RecordingHandler(Recorder recorder) : IIntegrationEventHandler<Pinged>
{
    public Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        recorder.Received.Enqueue(envelope);
        return Task.CompletedTask;
    }
}

public sealed class FailingHandler : IIntegrationEventHandler<Pinged>
{
    public Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("this handler always fails");
}

/// <summary>Publishes a follow-up event without saying which message caused it: the chain must follow by itself.</summary>
public sealed class ChainingHandler(Recorder recorder, IEventBus eventBus) : IIntegrationEventHandler<Pinged>
{
    public async Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        recorder.Received.Enqueue(envelope);
        await eventBus.PublishAsync(new Ponged(envelope.Message.Text), cancellationToken: cancellationToken);
    }
}

public sealed class FollowUpHandler(Recorder recorder) : IIntegrationEventHandler<Ponged>
{
    public Task HandleAsync(MessageEnvelope<Ponged> envelope, CancellationToken cancellationToken)
    {
        recorder.Followed.Enqueue(envelope);
        return Task.CompletedTask;
    }
}

public sealed class SlowHandler(Recorder recorder) : IIntegrationEventHandler<Pinged>
{
    public async Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        recorder.Enter();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
            recorder.Received.Enqueue(envelope);
        }
        finally
        {
            recorder.Leave();
        }
    }
}

/// <summary>Times out on its first call, as a handler waiting for a slow dependency does.</summary>
public sealed class TimeoutOnceHandler(Recorder recorder) : IIntegrationEventHandler<Pinged>
{
    public Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        recorder.Enter();
        recorder.Leave();
        if (recorder.Calls == 1)
        {
            throw new TimeoutException("the dependency did not answer");
        }

        recorder.Received.Enqueue(envelope);
        return Task.CompletedTask;
    }
}

public sealed class ReQueuedHandler(Recorder recorder) : IIntegrationEventHandler<ReQueuedEto>
{
    public Task HandleAsync(MessageEnvelope<ReQueuedEto> envelope, CancellationToken cancellationToken)
    {
        recorder.ReQueued.Enqueue(envelope);
        return Task.CompletedTask;
    }
}

/// <summary>Takes a long time over a <see cref="Pinged"/> and keeps its caller waiting until it is released.</summary>
public sealed class BlockedHandler(Recorder recorder, BlockedGate gate) : IIntegrationEventHandler<Pinged>
{
    public async Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        await gate.Released.Task.WaitAsync(cancellationToken);
        recorder.Received.Enqueue(envelope);
    }
}

public sealed class BlockedGate
{
    public TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Works on a <see cref="Pinged"/> for a while and says whether it was allowed to finish or was cancelled on the way.</summary>
public sealed class StopProbe
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool Completed { get; set; }

    public bool Cancelled { get; set; }
}

public sealed class LongRunningHandler(StopProbe probe) : IIntegrationEventHandler<Pinged>
{
    public async Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        probe.Started.SetResult();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            probe.Completed = true;
        }
        catch (OperationCanceledException)
        {
            probe.Cancelled = true;
            throw;
        }
    }
}

/// <summary>Works on a <see cref="Pinged"/> and takes no notice of its cancellation: what a handler that is stuck does.</summary>
public sealed class StubbornHandler(StopProbe probe) : IIntegrationEventHandler<Pinged>
{
    public async Task HandleAsync(MessageEnvelope<Pinged> envelope, CancellationToken cancellationToken)
    {
        probe.Started.SetResult();
        await Task.Delay(TimeSpan.FromSeconds(6), CancellationToken.None);
        probe.Completed = true;
    }
}
