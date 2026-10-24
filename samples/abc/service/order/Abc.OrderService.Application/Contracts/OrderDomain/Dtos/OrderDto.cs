namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderDto
{
    public Guid Id { get; init; }

    public string CustomerUserName { get; init; } = string.Empty;

    /// <summary>Pending, Confirmed, Shipped, Delivered or Cancelled.</summary>
    public string Status { get; init; } = string.Empty;

    public string? Note { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<OrderLineDto> Lines { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }

    public string? CreatedBy { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? UpdatedBy { get; init; }
}
