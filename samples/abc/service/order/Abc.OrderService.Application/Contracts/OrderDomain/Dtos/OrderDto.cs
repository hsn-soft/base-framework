namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderDto
{
    public Guid Id { get; init; }

    public string CustomerUserName { get; init; } = string.Empty;

    /// <summary>Pending, Confirmed, Shipped, Delivered or Cancelled.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The status in the language of the request (translated by the application service).</summary>
    public string StatusText { get; init; } = string.Empty;

    public string? Note { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<OrderLineDto> Lines { get; init; } = [];

    public Guid TenantId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public Guid? CreatedBy { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public Guid? UpdatedBy { get; init; }
}
