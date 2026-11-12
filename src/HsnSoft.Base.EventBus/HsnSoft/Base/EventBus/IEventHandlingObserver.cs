using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>How the handling of a message ended (a handling cut short by a shutdown has no outcome: its message comes back).</summary>
public enum EventHandlingOutcome
{
    /// <summary>The handlers ran to the end.</summary>
    Handled = 1,

    /// <summary>The application said no (a rule, a missing value): the message is done, it is not retried.</summary>
    Rejected = 2,

    /// <summary>The handler failed on every attempt: the failure is reported and the message is not handled.</summary>
    Failed = 3,
}

/// <summary>
/// What a package opens around the **whole handling of one message** (not each attempt of a handler: <see cref="IEventHandlerScope"/> is that): it begins when the handling starts and is told how it ended. The place of a concurrency limit that an endpoint handed over with the message
/// is kept alive and given back this way (<c>HsnSoft.Base.Concurrency.EventBus</c>). Register it as a <b>singleton</b> (<c>AddEventHandlingObserver</c>). A delivery that is left alone (the message was handled already, or another delivery is handling it) is not observed.
/// </summary>
public interface IEventHandlingObserver
{
    /// <summary>
    /// The handling starts. Returns what keeps the handling company until it ends (a heartbeat, say) and is disposed when it ends, whatever way (a shutdown too), or <see langword="null"/> when this message has nothing for the observer.
    /// </summary>
    ValueTask<IAsyncDisposable?> StartedAsync(ParentMessageEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>The handling ended with <paramref name="outcome"/> (it is not called for a handling cut short by a shutdown). A failure here is logged and never fails the handling.</summary>
    ValueTask EndedAsync(ParentMessageEnvelope envelope, EventHandlingOutcome outcome, CancellationToken cancellationToken);
}
