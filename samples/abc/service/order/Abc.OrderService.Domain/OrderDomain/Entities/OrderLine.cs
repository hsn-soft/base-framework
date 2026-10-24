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
        string name = productName?.Trim() ?? string.Empty;
        return name.Length == 0 || name.Length > OrderConsts.ProductNameMaxLength
            ? throw new DomainException($"The product name of a line is required and at most {OrderConsts.ProductNameMaxLength} characters.")
            : productId == Guid.Empty
                ? throw new DomainException("A line needs a product.")
                : unitPrice < 0
                    ? throw new DomainException("The unit price of a line cannot be negative.")
                    : quantity < 1
                        ? throw new DomainException("The quantity of a line is at least 1.")
                        : new OrderLine { ProductId = productId, ProductName = name, UnitPrice = unitPrice, Quantity = quantity };
    }
}
