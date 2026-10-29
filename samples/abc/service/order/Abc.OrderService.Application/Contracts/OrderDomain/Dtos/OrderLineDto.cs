namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderLineDto
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal Total { get; init; }
}
