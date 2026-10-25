using HsnSoft.Base.Application.Dtos;

namespace Xyz.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderDto : AuditedEntityDto<Guid>
{
    public string CustomerUserName { get; init; } = string.Empty;

    /// <summary>Pending, Confirmed, Shipped, Delivered or Cancelled.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The status in the language of the request (translated by the application service).</summary>
    public string StatusText { get; init; } = string.Empty;

    public string ShippingAddress { get; init; } = string.Empty;

    public string? Note { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<OrderLineDto> Lines { get; init; } = [];

    public Guid TenantId { get; init; }

}
