using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record ParentMessageEnvelope
{
    public ushort HopLevel { get; set; }

    public bool IsReQueued { get; set; }
    public ushort ReQueueCount { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string UserId { get; set; }

    [CanBeNull]
    public string UserRoleUniqueName { get; set; }

    [CanBeNull]
    public string Channel { get; set; }

    [CanBeNull]
    public string Producer { get; set; }
}