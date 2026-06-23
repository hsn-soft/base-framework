using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerVpSettingCheckDto : EntityDto<Guid>
{
    [NotNull] public string DomainName { get; set; } = string.Empty;
    [NotNull] public List<string> IncludePathFilters { get; set; } = [];
    [NotNull] public List<string> ExcludePathFilters { get; set; } = [];
}