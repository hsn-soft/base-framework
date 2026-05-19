using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;

public sealed class ClientDto : EntityDto<Guid>
{
    public Guid TenantId { get;  set; }

    [CanBeNull]
    public string SubdomainName { get;  set; }

    [NotNull]
    public string DomainName { get;  set; }

    public bool IsBlocked { get;  set; }

    public List<string> PathFilters { get; set; }

    public bool HasPathFilter => PathFilters is { Count: > 0 };
}