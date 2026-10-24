using Abc.OrderService.Domain.OrderDomain.Entities;

namespace Abc.OrderService.Domain.OrderDomain.Repositories;

/// <summary>Where the orders are kept. The domain says what it needs; the persistence project says how.</summary>
public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The orders that match <paramref name="filter"/>, the newest first, from <paramref name="skip"/> on, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<Order>> GetListAsync(OrderFilter filter, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>How many orders match <paramref name="filter"/>.</summary>
    Task<int> CountAsync(OrderFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The orders of the customers that have <paramref name="searchText"/> in their name (all of them when it is empty), the newest first, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<Order>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Saves the changes made to an order that <see cref="FindAsync"/> returned.</summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);
}
