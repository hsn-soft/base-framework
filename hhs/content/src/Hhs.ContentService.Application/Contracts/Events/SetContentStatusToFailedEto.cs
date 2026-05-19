using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.Events;

public sealed record SetContentStatusToFailedEto(ReferenceContentTypes ReferenceContentType, Guid ReferenceContentId, [CanBeNull] string FailedReason) : IIntegrationEventMessage
{
    public ReferenceContentTypes ReferenceContentType { get; } = ReferenceContentType;
    public Guid ReferenceContentId { get; } = ReferenceContentId;

    [CanBeNull]
    public string FailedReason { get; } = FailedReason;
}