using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public sealed class ParentMessageEnvelope
{
    public int HopLevel { get; set; }

    public Guid MessageId { get; set; }

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