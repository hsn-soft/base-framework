using System;

namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>
/// The record a service keeps of a message it has received, in its own database: what makes a message that comes twice (a broker delivers at least once) be handled once. The id is the id of the message, so the second
/// delivery finds the first. Only what is needed to decide is kept: not the message (it may hold personal data).
/// </summary>
public sealed class InboxMessage
{
    public const int EventNameMaxLength = 256;
    public const int CorrelationIdMaxLength = 128;
    public const int StatusMaxLength = 16;
    public const int ErrorMaxLength = 1000;

    public Guid Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    /// <summary>One of <see cref="InboxStatuses"/>.</summary>
    public string Status { get; set; } = InboxStatuses.Started;

    /// <summary>How many times the message was taken again after a failure or a handling that died.</summary>
    public int RetryCount { get; set; }

    /// <summary>When the handling that holds the message started (UTC): a handling that holds it longer than the lease is taken to be dead.</summary>
    public DateTime StartedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? Error { get; set; }
}
