using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerContentSettingDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }

    [NotNull] public string DomainName { get; set; }= string.Empty;

    public bool IsBlocked { get; set; }

    public List<string> PathFilters { get; set; } = [];

    public bool HasPathFilter => PathFilters is { Count: > 0 };
}