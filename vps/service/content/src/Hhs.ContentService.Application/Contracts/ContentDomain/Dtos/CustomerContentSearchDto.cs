using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class CustomerContentSearchDto : EntityDto<Guid>
{
    [NotNull] public string ScopeKey { get; set; } = string.Empty;

    [NotNull] public string SlugKey { get; set; } = string.Empty;
}