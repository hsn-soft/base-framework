using Abc.OrderService.Application.Contracts.Dtos;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Filters;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Services;

/// <summary>What can be done with the orders. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IOrderAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such order.</exception>
    Task<OrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the orders that match the filters, with how many match in all.</summary>
    Task<PagedDataResultDto<OrderDto>> GetPagedListAsync(GetOrdersPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the orders that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<OrderDto>> GetFilterListAsync(GetOrdersFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The orders that have the text in them, shown briefly.</summary>
    Task<List<OrderSearchDto>> GetSearchListAsync(GetOrdersSearch searchInput, CancellationToken cancellationToken = default);

    /// <summary>The customer is the signed-in user (the <c>sub</c> of the token).</summary>
    Task<OrderDto> CreateAsync(OrderCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such order.</exception>
    Task UpdateAsync(OrderUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such order.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
