using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record ParentMessageEnvelope
{
    public ushort HopLevel { get; set; }

    public ushort ReQueuedCount { get; set; }

    public Guid MessageId { get; set; }

    public DateTime MessageTime { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }

    [CanBeNull] public string UserId { get; set; }

    [CanBeNull] public string UserRoleUniqueName { get; set; }

    [CanBeNull] public string Channel { get; set; }

    [CanBeNull] public string Producer { get; set; }
}