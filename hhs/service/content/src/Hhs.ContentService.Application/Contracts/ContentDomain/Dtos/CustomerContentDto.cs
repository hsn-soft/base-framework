using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class CustomerContentDto : CreationAuditedEntityDto<Guid>
{
    [NotNull] public string ScopeKey { get; set; } = string.Empty;

    [NotNull] public string SlugKey { get; set; } = string.Empty;

    public CustomerContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public DateTime? ReleaseTime { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }
}