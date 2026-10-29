using System;

namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>What the inbox of a service decides with.</summary>
public sealed class EventInboxOptions
{
    /// <summary>How many times a message that failed is handled again when the broker brings it back (the error client puts it back). After that it is left to the error queue.</summary>
    public int MaxRetryCount { get; set; } = 4;

    /// <summary>
    /// How long a handling may hold a message before another delivery of it may take it over: a handling that crashed leaves the message <see cref="InboxStatuses.Started"/>, and the broker brings it back within seconds. Longer than the
    /// longest handler, or a slow handler is handled twice.
    /// </summary>
    public TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(2);
}
