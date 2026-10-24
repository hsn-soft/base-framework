using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.EntityFrameworkCore;
using Xyz.ServiceOrder.Host.Domain;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared.Events;

namespace Xyz.ServiceOrder.Host.Handlers;

// The results of the other services come back as events; each handler moves the order one step forward. Two results of the same order
// can arrive at the same moment: the one that loses the optimistic concurrency check reads the order again and applies its step on top.

internal static class OrderUpdates
{
    private const int Attempts = 5;

    /// <returns><see langword="false"/> when there is no such order.</returns>
    public static async Task<bool> ApplyAsync(OrderDbContext db, Guid orderId, Action<Order> step, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            Order? order = await db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
            if (order is null)
            {
                return false;
            }

            step(order);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException) when (attempt < Attempts)
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}

public sealed class StockReservedHandler(OrderDbContext db, ILogger<StockReservedHandler> logger) : IIntegrationEventHandler<StockReserved>
{
    public async Task HandleAsync(MessageEnvelope<StockReserved> envelope, CancellationToken cancellationToken)
    {
        if (!await OrderUpdates.ApplyAsync(db, envelope.Message.OrderId, order => order.ApplyStockReserved(envelope.Message.UnitPrice), cancellationToken))
        {
            logger.LogWarning("StockReserved for unknown order {OrderId}", envelope.Message.OrderId);
        }
    }
}

public sealed class StockRejectedHandler(OrderDbContext db, ILogger<StockRejectedHandler> logger) : IIntegrationEventHandler<StockRejected>
{
    public async Task HandleAsync(MessageEnvelope<StockRejected> envelope, CancellationToken cancellationToken)
    {
        if (!await OrderUpdates.ApplyAsync(db, envelope.Message.OrderId, order => order.ApplyStockRejected(envelope.Message.Reason), cancellationToken))
        {
            logger.LogWarning("StockRejected for unknown order {OrderId}", envelope.Message.OrderId);
        }
    }
}

public sealed class ShipmentDispatchedHandler(OrderDbContext db, ILogger<ShipmentDispatchedHandler> logger) : IIntegrationEventHandler<ShipmentDispatched>
{
    public async Task HandleAsync(MessageEnvelope<ShipmentDispatched> envelope, CancellationToken cancellationToken)
    {
        if (!await OrderUpdates.ApplyAsync(db, envelope.Message.OrderId, order => order.ApplyShipped(envelope.Message.TrackingNumber), cancellationToken))
        {
            logger.LogWarning("ShipmentDispatched for unknown order {OrderId}", envelope.Message.OrderId);
        }
    }
}
