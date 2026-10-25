using HsnSoft.Base;

namespace Xyz.OrderService.Domain.OrderDomain.Exceptions;

/// <summary>There is no such order: answered as 404.</summary>
public static class OrderNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Order {id} was not found.").WithKey("order_not_found", id);
}
