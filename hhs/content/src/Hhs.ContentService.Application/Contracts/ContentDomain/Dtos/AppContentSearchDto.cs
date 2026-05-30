using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class AppContentSearchDto : EntityDto<Guid>
{
    [NotNull] public string ClientDomainName { get; set; } = string.Empty;

    [NotNull] public string SlugKey { get; set; } = string.Empty;
}