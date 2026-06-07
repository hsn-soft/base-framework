using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.VideoGenerator;

public sealed record VideoGenerationStartedEto(Guid TenantId, Guid ClientId, [NotNull] string DomainName, ReferenceContentTypes ReferenceContentType, Guid ReferenceContentId, [NotNull] List<EncodedNormalizedContentData> EncodedNormalizedContentDatas) : IIntegrationEventMessage
{
    public Guid TenantId { get; } = TenantId;
    public Guid ClientId { get; } = ClientId;

    [NotNull]
    public string DomainName { get; } = DomainName;

    public ReferenceContentTypes ReferenceContentType { get; } = ReferenceContentType;
    public Guid ReferenceContentId { get; } = ReferenceContentId;

    [NotNull]
    public List<EncodedNormalizedContentData> EncodedNormalizedContentDatas { get; } = EncodedNormalizedContentDatas;
}

public sealed class EncodedNormalizedContentData
{
    public string EncodedTitleText { get; set; }

    [NotNull]
    public string EncodedNormalizedContent { get; set; }

    public string EncodedImageUrl { get; set; }
}