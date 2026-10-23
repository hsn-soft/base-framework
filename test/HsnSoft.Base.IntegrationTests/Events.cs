using System.Collections.Concurrent;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.IntegrationTests;

public sealed record Pinged(string Text) : IntegrationEvent;

public sealed class Recorder
{
    public ConcurrentQueue<Pinged> Received { get; } = new();
}

public sealed class RecordingHandler(Recorder recorder) : IIntegrationEventHandler<Pinged>
{
    public Task HandleAsync(Pinged integrationEvent, CancellationToken cancellationToken)
    {
        recorder.Received.Enqueue(integrationEvent);
        return Task.CompletedTask;
    }
}

public sealed class FailingHandler : IIntegrationEventHandler<Pinged>
{
    public Task HandleAsync(Pinged integrationEvent, CancellationToken cancellationToken)
        => throw new InvalidOperationException("this handler always fails");
}
