using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;

public sealed class ContentNormalizedRequestDto : CreationAuditedEntityDto<Guid>
{
    public Guid TenantId { get; set; }

    public Guid ClientId { get; set; }

    [NotNull] public string DomainName { get; set; } = string.Empty;

    public Guid AppContentId { get; set; }
    [NotNull] public string DomainPath { get; set; } = string.Empty;

    public ContentNormalizedRequestStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    [CanBeNull] public ScrapingContentDataModel ScrapingContentData { get; set; }

    [CanBeNull] public string OutlineContentData { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }
}