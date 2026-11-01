using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos;

public sealed class CompanyDto : AuditedEntityDto<Guid>
{
    public string Title { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
