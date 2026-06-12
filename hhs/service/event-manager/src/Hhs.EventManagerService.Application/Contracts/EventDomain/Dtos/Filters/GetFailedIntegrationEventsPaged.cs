using Hhs.EventManagerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Filters;

public sealed class GetFailedIntegrationEventsPaged : PagedDataRequestDto
{
    public DateTime? EnvelopeTime { get; set; }

    public FailedIntegrationEventStates? OperationStatus { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string Producer { get; set; }

    [CanBeNull]
    public string FailedMessageTypeName { get; set; }

    public bool? IsReQueued { get; set; }
}