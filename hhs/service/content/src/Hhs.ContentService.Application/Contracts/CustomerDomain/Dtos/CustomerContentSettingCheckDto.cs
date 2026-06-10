using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerContentSettingCheckDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }
    [NotNull] public string DomainName { get; set; } = string.Empty;

    public List<KeyValuePair<CustomerSettingFilterTypes, string>> PathFilters { get; set; }
}