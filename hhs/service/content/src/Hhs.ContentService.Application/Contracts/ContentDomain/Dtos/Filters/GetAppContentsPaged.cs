using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;

public sealed class GetAppContentsPaged : PagedDataRequestDto
{
    public Guid? ClientId { get; set; }

    public DateTime? CreationTimeStart { get; set; }
    public DateTime? CreationTimeEnd { get; set; }

    [CanBeNull] public string SlugKey { get; set; }

    public AppContentOperationStates? OperationStatus { get; set; }

    public DateTime? ReleaseTimeStart { get; set; }
    public DateTime? ReleaseTimeEnd { get; set; }
}