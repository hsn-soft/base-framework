namespace Xyz.ServiceOrder.Host.Domain;

public enum OrderStatus
{
    /// <summary>Saved, waiting for the stock check.</summary>
    Pending,

    /// <summary>The stock is reserved, the goods wait for the shipment.</summary>
    StockReserved,

    /// <summary>The order cannot be fulfilled (see <see cref="Order.RejectReason"/>).</summary>
    Rejected,

    /// <summary>The goods left the warehouse (see <see cref="Order.TrackingNumber"/>).</summary>
    Shipped,
}

public sealed class Order
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string CustomerId { get; private set; } = string.Empty;

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal? TotalPrice { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public string? RejectReason { get; private set; }

    public string? TrackingNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>PostgreSQL system column <c>xmin</c>: two handlers updating the same order cannot overwrite each other.</summary>
    public uint Version { get; private set; }

    public static Order Place(string customerId, Guid productId, int quantity)
        => new() { CustomerId = customerId, ProductId = productId, Quantity = quantity };

    // The events of one order reach this service through different queues, so they can arrive in any order.
    // Each transition therefore only moves the order forward and ignores what no longer applies.

    public void ApplyStockReserved(decimal unitPrice)
    {
        TotalPrice = unitPrice * Quantity;
        if (Status == OrderStatus.Pending)
        {
            Status = OrderStatus.StockReserved;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyStockRejected(string reason)
    {
        if (Status != OrderStatus.Pending)
        {
            return;
        }

        Status = OrderStatus.Rejected;
        RejectReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyShipped(string trackingNumber)
    {
        if (Status == OrderStatus.Rejected)
        {
            return;
        }

        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
