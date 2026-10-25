using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;

namespace Abc.OrderService.Domain.OrderDomain.Entities;

/// <summary>One product of an order, with the name and the price it had when it was ordered (the product service may change them later).</summary>
public sealed class OrderLine
{
    // for EF Core
    private OrderLine()
    {
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal Total => UnitPrice * Quantity;

    public static OrderLine Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        return new OrderLine
        {
            ProductId = Guard.NotDefault<Guid>(productId, nameof(ProductId)),
            ProductName = Guard.NotNullOrWhiteSpace(productName, nameof(ProductName), OrderConsts.ProductNameMaxLength),
            UnitPrice = Guard.NotNegative(unitPrice, nameof(UnitPrice)),
            Quantity = Guard.Range(quantity, nameof(Quantity), 1, int.MaxValue),
        };
    }
}
