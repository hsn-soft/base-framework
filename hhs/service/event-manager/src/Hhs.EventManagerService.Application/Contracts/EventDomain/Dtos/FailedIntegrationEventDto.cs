using Hhs.EventManagerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos;

public sealed class FailedIntegrationEventDto : EntityDto<Guid>
{
    public DateTime EnvelopeTime { get; set; }

    [NotNull]
    public string FailedReason { get; set; }

    public FailedIntegrationEventStates OperationStatus { get; set; }

    [CanBeNull]
    public string OperationStatusDescription { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string Producer { get; set; }

    [CanBeNull]
    public string FailedMessageTypeName { get; set; }

    public bool IsReQueued { get; set; }
}