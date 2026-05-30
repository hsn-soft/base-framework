using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;

public sealed class ClientSearchDto : EntityDto<Guid>
{
    [NotNull] public string DomainName { get; set; } = string.Empty;
}