using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos.Filters;

public sealed class GetClientsFilter : SortedAndLimitedDataRequestDto
{
    public Guid? TenantId { get; set; }

    [CanBeNull]
    public string DomainName { get; set; }

    public bool? IsBlocked { get; set; }
}