using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;

public sealed class ClientSearchDto : EntityDto<Guid>
{
    [NotNull]
    public string DomainName { get; set; }
}