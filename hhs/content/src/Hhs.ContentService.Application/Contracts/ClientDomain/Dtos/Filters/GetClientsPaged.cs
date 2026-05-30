using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Filters;

public sealed class GetClientsPaged : PagedDataRequestDto
{
    public Guid? TenantId { get; set; }

    [CanBeNull] public string DomainName { get; set; }

    public bool? IsBlocked { get; set; }
}