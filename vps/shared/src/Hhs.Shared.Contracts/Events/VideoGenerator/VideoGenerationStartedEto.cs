using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.VideoGenerator;

public sealed record VideoGenerationStartedEto(
    [NotNull] string ScopeKey,
    [NotNull] string DomainName,
    ReferenceContentTypes ReferenceContentType,
    Guid ReferenceContentId,
    [NotNull] List<EncodedNormalizedContentData> EncodedNormalizedContentDatas
) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;

    [NotNull] public string DomainName { get; } = DomainName;

    public ReferenceContentTypes ReferenceContentType { get; } = ReferenceContentType;
    public Guid ReferenceContentId { get; } = ReferenceContentId;

    [NotNull] public List<EncodedNormalizedContentData> EncodedNormalizedContentDatas { get; } = EncodedNormalizedContentDatas;
}

public sealed class EncodedNormalizedContentData
{
    public string EncodedTitleText { get; set; }

    [NotNull] public string EncodedNormalizedContent { get; set; }

    public string EncodedImageUrl { get; set; }
}