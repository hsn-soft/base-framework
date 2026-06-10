using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerContentSettingSearchDto : EntityDto<Guid>
{
    [NotNull] public string DomainName { get; set; } = string.Empty;
}