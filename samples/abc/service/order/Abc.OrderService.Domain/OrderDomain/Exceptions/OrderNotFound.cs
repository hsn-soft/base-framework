using HsnSoft.Base;

namespace Abc.OrderService.Domain.OrderDomain.Exceptions;

/// <summary>There is no such order: answered as 404.</summary>
public static class OrderNotFound
{
    public static NotFoundException For(Guid id) => new($"Order {id} was not found.");
}
