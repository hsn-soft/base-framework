using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Repositories;
using Abc.OrderService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Abc.OrderService.EntityFrameworkCore.Repositories;

public sealed class EfCoreOrderRepository(OrderDbContext db) : IOrderRepository
{
    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetListAsync(OrderFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        => await Filter(filter).AsNoTracking().Include(order => order.Lines).OrderByDescending(order => order.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountAsync(OrderFilter filter, CancellationToken cancellationToken = default) => Filter(filter).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string text = Order.Normalize(searchText);
            query = query.Where(order => order.NormalizedCustomerUserName.Contains(text));
        }

        return await query.OrderByDescending(order => order.CreatedAt).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await db.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        db.Orders.Remove(order);
        return db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Order> Filter(OrderFilter filter)
    {
        IQueryable<Order> query = db.Orders;
        if (!string.IsNullOrWhiteSpace(filter.CustomerUserName))
        {
            string name = Order.Normalize(filter.CustomerUserName);
            query = query.Where(order => order.NormalizedCustomerUserName.Contains(name));
        }

        return filter.Status is { } status ? query.Where(order => order.Status == status) : query;
    }
}
