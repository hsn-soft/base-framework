using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class AppContentDto : CreationAuditedEntityDto<Guid>
{
    public Guid TenantId { get; set; }

    public Guid ClientId { get; set; }

    [NotNull] public string ClientDomainName { get; set; } = string.Empty;

    [NotNull] public string SlugKey { get; set; } = string.Empty;

    public AppContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public DateTime? ReleaseTime { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }
}