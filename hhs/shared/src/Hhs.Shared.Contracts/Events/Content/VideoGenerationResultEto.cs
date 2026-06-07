using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record VideoGenerationResultEto(ReferenceContentTypes ReferenceContentType, Guid ReferenceContentId, bool IsGenerateSuccess, Guid VideoRequestId, [CanBeNull] string StorageVideoUrl) : IIntegrationEventMessage
{
    public ReferenceContentTypes ReferenceContentType { get; } = ReferenceContentType;
    public Guid ReferenceContentId { get; } = ReferenceContentId;

    public bool IsGenerateSuccess { get; } = IsGenerateSuccess;

    public Guid VideoRequestId { get; } = VideoRequestId;

    [CanBeNull]
    public string StorageVideoUrl { get; } = StorageVideoUrl;
}