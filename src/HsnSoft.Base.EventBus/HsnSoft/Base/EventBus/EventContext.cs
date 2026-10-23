using System;
using System.Threading;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>The message a handler is handling right now, available to everything the handler calls (it follows async calls).</summary>
public static class EventContext
{
    private static readonly AsyncLocal<ParentMessageEnvelope?> s_current = new();

    /// <summary>The envelope of the message being handled, or <see langword="null"/> outside a handler.</summary>
    public static ParentMessageEnvelope? Current => s_current.Value;

    /// <summary>Makes <paramref name="envelope"/> the current message until the returned scope is disposed.</summary>
    public static IDisposable Use(ParentMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ParentMessageEnvelope? previous = s_current.Value;
        s_current.Value = envelope;
        return new Restore(previous);
    }

    private sealed class Restore(ParentMessageEnvelope? previous) : IDisposable
    {
        public void Dispose() => s_current.Value = previous;
    }
}
