using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;

/// <summary>What a caller may filter its subscriptions by: the companies, the customers and the products that are in the subscriptions it sees (a reseller has no other way to the catalogue).</summary>
public sealed class SubscriptionFiltersDto
{
    public IReadOnlyList<FilterCompanyDto> Companies { get; init; } = [];

    public IReadOnlyList<FilterCustomerDto> Customers { get; init; } = [];

    public IReadOnlyList<ProductTypes> Products { get; init; } = [];
}

public sealed class FilterCompanyDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;
}

public sealed class FilterCustomerDto
{
    public Guid Id { get; init; }

    public Guid CompanyId { get; init; }

    public string Domain { get; init; } = string.Empty;
}
