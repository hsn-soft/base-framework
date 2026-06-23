using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;

public sealed class ContentNormalizedRequestSearchDto : EntityDto<Guid>
{
    [NotNull] public string DomainName { get; set; } = string.Empty;

    [NotNull] public string DomainPath { get; set; } = string.Empty;
}