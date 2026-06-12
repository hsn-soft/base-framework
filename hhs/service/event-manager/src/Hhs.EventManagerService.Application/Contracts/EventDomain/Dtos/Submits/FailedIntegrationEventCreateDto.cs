using System.ComponentModel.DataAnnotations;
using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;

namespace Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Submits;

public sealed class FailedIntegrationEventCreateDto : IValidatableObject
{
    public Guid? EnvelopeId { get; set; }

    public DateTime? EnvelopeTime { get; set; }

    [CanBeNull]
    public string FailedReason { get; set; }

    public FailedIntegrationEventStates? OperationStatus { get; set; }

    [CanBeNull]
    public string OperationStatusDescription { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string Producer { get; set; }

    [CanBeNull]
    public string Channel { get; set; }

    [CanBeNull]
    public string UserId { get; set; }

    [CanBeNull]
    public string UserRoleUniqueName { get; set; }

    [CanBeNull]
    public DateTime? FailedMessageEnvelopeTime { get; set; }

    [CanBeNull]
    public object FailedMessageObject { get; set; }

    [CanBeNull]
    public string FailedMessageTypeName { get; set; }

    public ushort HopLevel { get; set; }

    public bool IsReQueued { get; set; }
    public ushort ReQueueCount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnvelopeId.HasValue)
        {
            if (EnvelopeId.Value == default)
            {
                yield return new ValidationResult(ValidationResourceKeys.IsNotEmpty, new[] { "FailedIntegrationEvent:EnvelopeId" });
            }
        }

        if (!EnvelopeTime.HasValue)
        {
            yield return new ValidationResult(ValidationResourceKeys.IsNotEmpty, new[] { "FailedIntegrationEvent:EnvelopeTime" });
        }
        else
        {
            if (EnvelopeTime.Value > DateTime.UtcNow || EnvelopeTime.Value == default)
            {
                yield return new ValidationResult(ValidationResourceKeys.IsNotEmpty, new[] { "FailedIntegrationEvent:EnvelopeTime" });
            }
        }

        if (!CheckSafe.NotNullOrEmpty(FailedReason, nameof(FailedReason)))
        {
            yield return new ValidationResult(ValidationResourceKeys.Required, new[] { "FailedIntegrationEvent:FailedReason" });
        }

        if (!OperationStatus.HasValue)
        {
            yield return new ValidationResult(ValidationResourceKeys.IsNotEmpty, new[] { "FailedIntegrationEvent:OperationStatus" });
        }

        if (!string.IsNullOrWhiteSpace(CorrelationId))
        {
            if (!CheckSafe.Length(CorrelationId, nameof(CorrelationId), FailedIntegrationEventConsts.CorrelationIdMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:CorrelationId" });
            }
        }

        if (!string.IsNullOrWhiteSpace(Producer))
        {
            if (!CheckSafe.Length(Producer, nameof(Producer), FailedIntegrationEventConsts.ProducerMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:Producer" });
            }
        }

        if (!string.IsNullOrWhiteSpace(Channel))
        {
            if (!CheckSafe.Length(Channel, nameof(Channel), FailedIntegrationEventConsts.ChannelMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:Channel" });
            }
        }

        if (!string.IsNullOrWhiteSpace(UserId))
        {
            if (!CheckSafe.Length(UserId, nameof(UserId), FailedIntegrationEventConsts.UserIdMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:UserId" });
            }
        }

        if (!string.IsNullOrWhiteSpace(UserRoleUniqueName))
        {
            if (!CheckSafe.Length(UserRoleUniqueName, nameof(UserRoleUniqueName), FailedIntegrationEventConsts.UserRoleUniqueNameMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:UserRoleUniqueName" });
            }
        }

        if (FailedMessageEnvelopeTime.HasValue)
        {
            if (FailedMessageEnvelopeTime.Value > DateTime.UtcNow || FailedMessageEnvelopeTime.Value == default)
            {
                yield return new ValidationResult(ValidationResourceKeys.IsNotEmpty, new[] { "FailedIntegrationEvent:FailedMessageEnvelopeTime" });
            }
        }

        if (!string.IsNullOrWhiteSpace(FailedMessageTypeName))
        {
            if (!CheckSafe.Length(FailedMessageTypeName, nameof(FailedMessageTypeName), FailedIntegrationEventConsts.FailedMessageTypeNameMaxLength))
            {
                yield return new ValidationResult(ValidationResourceKeys.MaxLength, new[] { "FailedIntegrationEvent:FailedMessageTypeName" });
            }
        }
    }
}