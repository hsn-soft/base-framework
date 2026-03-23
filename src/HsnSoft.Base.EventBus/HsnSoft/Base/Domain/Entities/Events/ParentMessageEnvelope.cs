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
    [CanBeNull] public string UserRoles { get; set; }
    [CanBeNull] public string ClientLat { get; set; }
    [CanBeNull] public string ClientLong { get; set; }
    [CanBeNull] public string ClientChannel { get; set; }
    [CanBeNull] public string ClientVersion { get; set; }

    [CanBeNull] public string Producer { get; set; }
}