using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.Events;

public sealed record SetNormalizedStatusToFailedEto(ReferenceContentTypes ReferenceNormalizedType, Guid ReferenceNormalizedId, [CanBeNull] string FailedReason) : IIntegrationEventMessage
{
    public ReferenceContentTypes ReferenceNormalizedType { get; } = ReferenceNormalizedType;
    public Guid ReferenceNormalizedId { get; } = ReferenceNormalizedId;

    [CanBeNull]
    public string FailedReason { get; } = FailedReason;
}