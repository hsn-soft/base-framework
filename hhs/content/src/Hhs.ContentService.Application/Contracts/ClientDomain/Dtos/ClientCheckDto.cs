using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;

public sealed class ClientCheckDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }
    [NotNull] public string DomainName { get; set; }

    public List<KeyValuePair<ClientFilterTypes, string>> PathFilters { get; set; }
}