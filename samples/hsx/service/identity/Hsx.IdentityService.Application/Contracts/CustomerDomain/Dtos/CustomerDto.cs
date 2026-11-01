using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerDto : AuditedEntityDto<Guid>
{
    public Guid CompanyId { get; init; }
    public string Domain { get; init; } = string.Empty;
}
