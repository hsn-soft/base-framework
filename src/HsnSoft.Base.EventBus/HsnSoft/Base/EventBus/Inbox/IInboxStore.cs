using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>What <see cref="IInboxStore.TryStartAsync"/> decided.</summary>
public enum InboxStart
{
    /// <summary>This handling owns the message: the handler runs.</summary>
    Started,

    /// <summary>The message was handled before: it is not handled again.</summary>
    AlreadyHandled,

    /// <summary>Another handling holds the message and its lease has not run out: this delivery is left alone.</summary>
    InProgress,

    /// <summary>The message failed more often than the options allow: it is left to the error queue.</summary>
    RetryLimitReached,
}

/// <summary>
/// The inbox of a service: the record of the messages it has received, kept where the service keeps its data. Every call is one statement of the store and works for any number of handlings at the same time: of two deliveries of one
/// message at once, only one gets <see cref="InboxStart.Started"/>.
/// </summary>
public interface IInboxStore
{
    /// <summary>Takes the message for a handling, or says why it is not taken. A failure of the store itself is thrown: the message must not be dropped because the inbox could not be asked.</summary>
    Task<InboxStart> TryStartAsync(Guid messageId, string eventName, string? correlationId, CancellationToken cancellationToken = default);

    /// <summary>The handling ended well (or the application said no): the message is never handled again.</summary>
    Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>The handling failed: the message may be handled again when it comes back.</summary>
    Task FailAsync(Guid messageId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>Said by <c>AddEventInbox</c>: the service has an inbox, and the dispatcher uses it.</summary>
public sealed class InboxEnabled;
