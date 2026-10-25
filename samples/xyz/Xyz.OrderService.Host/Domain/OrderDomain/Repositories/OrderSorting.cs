using HsnSoft.Base.Domain.Sorting;
using Xyz.OrderService.Domain.OrderDomain.Entities;

namespace Xyz.OrderService.Domain.OrderDomain.Repositories;

/// <summary>The fields a list of orders may be sorted by (the names a client sends, <c>"totalAmount desc"</c>) and the sorting when it sends none: the newest first.</summary>
public static class OrderSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<Order> Fields = new SortFields<Order>()
        .Add("createdAt", order => order.CreatedAt)
        .Add("customer", order => order.NormalizedCustomerUserName)
        .Add("status", order => order.Status)
        .Add("totalAmount", order => order.TotalAmount);
}
