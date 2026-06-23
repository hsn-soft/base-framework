using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.EventManagerService.Domain.EventDomain.Entities;

public sealed class FailedIntegrationEvent : CreationAuditedEntity<Guid>
{
    public DateTime EnvelopeTime { get; private set; }

    [NotNull]
    public string FailedReason { get; private set; }

    public FailedIntegrationEventStates OperationStatus { get; internal set; }

    [CanBeNull]
    public string OperationStatusDescription { get; internal set; }

    [CanBeNull]
    public string CorrelationId { get; private set; }

    [CanBeNull]
    public string Producer { get; private set; }

    [CanBeNull]
    public string Channel { get; private set; }

    [CanBeNull]
    public string UserId { get; private set; }

    [CanBeNull]
    public string UserRoleUniqueName { get; private set; }

    [CanBeNull]
    public Guid? FailedMessageEnvelopeId { get; private set; }

    [CanBeNull]
    public DateTime? FailedMessageEnvelopeTime { get; private set; }

    [CanBeNull]
    public object FailedMessageObject { get; internal set; }

    [CanBeNull]
    public string FailedMessageTypeName { get; private set; }

    public ushort HopLevel { get; internal set; }

    public ushort ReQueuedCount { get; internal set; }

    private FailedIntegrationEvent()
    {
        FailedReason = string.Empty;
    }

    internal FailedIntegrationEvent(Guid id,
        DateTime envelopeTime,
        [NotNull] string failedReason,
        FailedIntegrationEventStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null,
        [CanBeNull] string producer = null,
        [CanBeNull] string channel = null,
        [CanBeNull] string userId = null,
        [CanBeNull] string userRoleUniqueName = null,
        [CanBeNull] Guid? failedMessageEnvelopeId = null,
        [CanBeNull] DateTime? failedMessageEnvelopeTime = null,
        [CanBeNull] object failedMessageObject = null,
        [CanBeNull] string failedMessageTypeName = null,
        ushort hopLevel = 1,
        ushort reQueuedCount = 0
    ) : this()
    {
        Id = id;
        SetEnvelopeTime(envelopeTime);
        SetFailedReason(failedReason);
        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;
        SetCorrelationId(correlationId);
        SetProducer(producer);
        SetChannel(channel);
        SetUserId(userId);
        SetUserRoleUniqueName(userRoleUniqueName);

        FailedMessageEnvelopeId = failedMessageEnvelopeId;
        SetFailedMessageEnvelopeTime(failedMessageEnvelopeTime);
        FailedMessageObject = failedMessageObject;
        SetFailedMessageTypeName(failedMessageTypeName);

        HopLevel = hopLevel;
        ReQueuedCount = reQueuedCount;
    }

    internal void SetEnvelopeTime(DateTime envelopeTime)
    {
        if (envelopeTime > DateTime.Now || envelopeTime == default)
        {
            throw new ArgumentException("EnvelopeTime is invalid", nameof(envelopeTime));
        }

        EnvelopeTime = envelopeTime.ToUniversalTime();
    }

    internal void SetFailedReason(string failedReason)
    {
        FailedReason = Check.NotNull(failedReason, nameof(failedReason));
    }

    internal void SetCorrelationId(string correlationId)
    {
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? null
            : Check.Length(correlationId, nameof(correlationId), FailedIntegrationEventConsts.CorrelationIdMaxLength);
    }

    internal void SetProducer(string producer)
    {
        Producer = string.IsNullOrWhiteSpace(producer)
            ? null
            : Check.Length(producer, nameof(producer), FailedIntegrationEventConsts.ProducerMaxLength);
    }

    internal void SetChannel(string channel)
    {
        Channel = string.IsNullOrWhiteSpace(channel)
            ? null
            : Check.Length(channel, nameof(channel), FailedIntegrationEventConsts.ChannelMaxLength);
    }

    internal void SetUserId(string userId)
    {
        UserId = string.IsNullOrWhiteSpace(userId)
            ? null
            : Check.Length(userId, nameof(userId), FailedIntegrationEventConsts.UserIdMaxLength);
    }

    internal void SetUserRoleUniqueName(string userRoleUniqueName)
    {
        UserRoleUniqueName = string.IsNullOrWhiteSpace(userRoleUniqueName)
            ? null
            : Check.Length(userRoleUniqueName, nameof(userRoleUniqueName), FailedIntegrationEventConsts.UserRoleUniqueNameMaxLength);
    }


    internal void SetFailedMessageEnvelopeTime(DateTime? failedMessageEnvelopeTime)
    {
        if (failedMessageEnvelopeTime.HasValue)
        {
            if (failedMessageEnvelopeTime.Value > DateTime.Now || failedMessageEnvelopeTime.Value == default)
            {
                throw new ArgumentException("FailedMessageEnvelopeTime is invalid", nameof(failedMessageEnvelopeTime));
            }

            failedMessageEnvelopeTime = failedMessageEnvelopeTime.Value.ToUniversalTime();
        }

        FailedMessageEnvelopeTime = failedMessageEnvelopeTime;
    }

    internal void SetFailedMessageTypeName(string failedMessageTypeName)
    {
        FailedMessageTypeName = string.IsNullOrWhiteSpace(failedMessageTypeName)
            ? null
            : Check.Length(failedMessageTypeName, nameof(failedMessageTypeName), FailedIntegrationEventConsts.FailedMessageTypeNameMaxLength);
    }
}