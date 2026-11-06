using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;

public sealed class FilterCompanyDto : IBaseDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;
}
