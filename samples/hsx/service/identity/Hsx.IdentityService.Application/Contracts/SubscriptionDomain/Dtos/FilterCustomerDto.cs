using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;

public sealed class FilterCustomerDto : IBaseDto
{
    public Guid Id { get; init; }

    public Guid CompanyId { get; init; }

    public string Domain { get; init; } = string.Empty;
}
