using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.Logging;

public sealed record ProduceMessageLogModel(
    string LogId,
    [CanBeNull] string CorrelationId,
    string Facility,
    DateTimeOffset ProduceDateTimeUtc,
    MessageLogDetail MessageLog,
    [CanBeNull] string ProduceDetails
) : IEventBusLog
{
    public string LogId { get; } = LogId;

    [CanBeNull]
    public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;

    public DateTimeOffset ProduceDateTimeUtc { get; } = ProduceDateTimeUtc;

    [CanBeNull]
    public MessageLogDetail MessageLog { get; } = MessageLog;

    [CanBeNull]
    public string ProduceDetails { get; } = ProduceDetails;
}

public sealed record ConsumeMessageLogModel(
    string LogId,
    [CanBeNull] string CorrelationId,
    string Facility,
    DateTimeOffset ConsumeDateTimeUtc,
    MessageLogDetail MessageLog,
    [CanBeNull] string ConsumeDetails,
    [CanBeNull] string ConsumeHandleWorkingTime
) : IEventBusLog
{
    public string LogId { get; } = LogId;

    [CanBeNull]
    public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;

    public DateTimeOffset ConsumeDateTimeUtc { get; } = ConsumeDateTimeUtc;

    [CanBeNull]
    public MessageLogDetail MessageLog { get; } = MessageLog;

    [CanBeNull]
    public string ConsumeDetails { get; } = ConsumeDetails;

    [CanBeNull]
    public string ConsumeHandleWorkingTime { get; } = ConsumeHandleWorkingTime;
}

public sealed record MessageLogDetail(
    string EventType,
    int HopLevel,
    Guid? ParentMessageId,
    Guid MessageId,
    DateTimeOffset MessageTime,
    dynamic Message,
    [CanBeNull] EventUserDetail UserInfo)
{
    public string EventType { get; } = EventType;
    public int HopLevel { get; } = HopLevel;
    public Guid? ParentMessageId { get; } = ParentMessageId;
    public Guid MessageId { get; } = MessageId;
    public DateTimeOffset MessageTime { get; } = MessageTime;
    public dynamic Message { get; } = Message;

    [CanBeNull]
    public EventUserDetail UserInfo { get; } = UserInfo;
}

public sealed record EventUserDetail([CanBeNull] string UserId, [CanBeNull] string Role)
{
    [CanBeNull]
    public string UserId { get; } = UserId;

    [CanBeNull]
    public string Role { get; } = Role;
}

// Marker
public interface IEventBusLog
{
}