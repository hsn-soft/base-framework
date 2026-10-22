using System.Collections.Concurrent;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.Sample.Api.EventHandlers;

/// <summary>Keeps the most recent events handled by this instance so they can be inspected (diagnostics, tests).</summary>
public sealed class ReceivedEventStore
{
    private readonly ConcurrentQueue<object> _events = new();

    public IReadOnlyCollection<MessageEnvelope<T>> Get<T>() where T : IIntegrationEventMessage
        => _events.OfType<MessageEnvelope<T>>().ToList();

    public void Add<T>(MessageEnvelope<T> envelope) where T : IIntegrationEventMessage => _events.Enqueue(envelope);
}
