using HsnSoft.Base.Application.Dtos;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Submits;

namespace Xyz.ShipmentService.Application.Contracts.CustomerDomain.Services;

/// <summary>What can be done with the customers. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICustomerAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync(GetCustomersPaged pagedInput, CancellationToken cancellationToken = default);

    Task<List<CustomerDto>> GetFilterListAsync(GetCustomersFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The customers that have the text in their user name or full name, shown briefly.</summary>
    Task<List<CustomerSearchDto>> GetSearchListAsync(GetCustomersSearch searchInput, CancellationToken cancellationToken = default);

    /// <exception cref="Xyz.ShipmentService.Domain.CustomerDomain.Exceptions.CustomerUserNameDuplicateException">There is a customer with this user name.</exception>
    Task<CustomerDto> CreateAsync(CustomerCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// The customer of a user: the one with this user name when there is one, else one made now (its full name is the user name, its address the given one, its user id the id of the user who is acting). What an order of a user the service does
    /// not know yet needs before it can be paid.
    /// </summary>
    Task<CustomerDto> EnsureAsync(string userName, string? address, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    Task UpdateAsync(CustomerUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
