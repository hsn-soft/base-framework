using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class AppContentDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }

    [CanBeNull]
    public ClientSearchDto Client { get; set; }

    [NotNull]
    public string SlugKey { get; set; }

    public AppContentOperationStates OperationStatus { get; set; }

    [CanBeNull]
    public string OperationStatusDescription { get; set; }

    public Guid? NormalizedRequestId { get; set; }

    public DateTime? ReleaseTime{ get; set; }

    public Guid? VideoRequestId { get; set; }

    [CanBeNull]
    public string StorageVideoUrl { get; set; }
}