using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Events;

public sealed record SetVideoRequestStatusToFailedEto(Guid VideoRequestId, [CanBeNull] string FailedReason) : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; } = VideoRequestId;

    [CanBeNull]
    public string FailedReason { get; } = FailedReason;
}