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

    [CanBeNull] public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;

    public DateTimeOffset ProduceDateTimeUtc { get; } = ProduceDateTimeUtc;

    [CanBeNull] public MessageLogDetail MessageLog { get; } = MessageLog;

    [CanBeNull] public string ProduceDetails { get; } = ProduceDetails;
}

public sealed record ConsumeMessageLogModel(
    string LogId,
    [CanBeNull] string CorrelationId,
    string Facility,
    string Producer,
    DateTimeOffset ConsumeDateTimeUtc,
    MessageLogDetail MessageLog,
    [CanBeNull] string ConsumeDetails,
    long ConsumeHandleWorkingTimeMs
) : IEventBusLog
{
    public string LogId { get; } = LogId;

    [CanBeNull] public string CorrelationId { get; } = CorrelationId;

    public string Facility { get; } = Facility;
    public string Producer { get; } = Producer;

    public DateTimeOffset ConsumeDateTimeUtc { get; } = ConsumeDateTimeUtc;

    [CanBeNull] public MessageLogDetail MessageLog { get; } = MessageLog;

    [CanBeNull] public string ConsumeDetails { get; } = ConsumeDetails;

    public long ConsumeHandleWorkingTimeMs { get; } = ConsumeHandleWorkingTimeMs;
}

public sealed record MessageLogDetail(
    string EventType,
    int HopLevel,
    Guid? ParentMessageId,
    Guid MessageId,
    DateTimeOffset MessageTime,
    dynamic Message,
    [CanBeNull] string UserId,
    [CanBeNull] string UserRoles,
    [CanBeNull] string ClientLat,
    [CanBeNull] string ClientLong,
    [CanBeNull] string ClientChannel,
    [CanBeNull] string ClientVersion
)
{
    public string EventType { get; } = EventType;
    public int HopLevel { get; } = HopLevel;
    public Guid? ParentMessageId { get; } = ParentMessageId;
    public Guid MessageId { get; } = MessageId;
    public DateTimeOffset MessageTime { get; } = MessageTime;
    public dynamic Message { get; } = Message;

    [CanBeNull] public string UserId { get; } = UserId;
    [CanBeNull] public string UserRoles { get; } = UserRoles;
    [CanBeNull] public string ClientLat { get; set; } = ClientLat;
    [CanBeNull] public string ClientLong { get; set; } = ClientLong;
    [CanBeNull] public string ClientChannel { get; set; } = ClientChannel;
    [CanBeNull] public string ClientVersion { get; set; } = ClientVersion;
}