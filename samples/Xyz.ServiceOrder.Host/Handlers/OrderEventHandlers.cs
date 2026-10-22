using Microsoft.EntityFrameworkCore;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared.Events;
using HsnSoft.Base.EventBus;

namespace Xyz.ServiceOrder.Host.Handlers;

// The results of the other services come back as events; each handler moves the order one step forward.
// A concurrent update of the same order throws DbUpdateConcurrencyException; the subscriber simply retries the handler.

public sealed class StockReservedHandler(OrderDbContext db, ILogger<StockReservedHandler> logger) : IIntegrationEventHandler<StockReserved>
{
    public async Task HandleAsync(StockReserved @event, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == @event.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockReserved for unknown order {OrderId}", @event.OrderId);
            return;
        }

        order.ApplyStockReserved(@event.UnitPrice);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StockRejectedHandler(OrderDbContext db, ILogger<StockRejectedHandler> logger) : IIntegrationEventHandler<StockRejected>
{
    public async Task HandleAsync(StockRejected @event, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == @event.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockRejected for unknown order {OrderId}", @event.OrderId);
            return;
        }

        order.ApplyStockRejected(@event.Reason);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ShipmentDispatchedHandler(OrderDbContext db, ILogger<ShipmentDispatchedHandler> logger) : IIntegrationEventHandler<ShipmentDispatched>
{
    public async Task HandleAsync(ShipmentDispatched @event, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == @event.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("ShipmentDispatched for unknown order {OrderId}", @event.OrderId);
            return;
        }

        order.ApplyShipped(@event.TrackingNumber);
        await db.SaveChangesAsync(cancellationToken);
    }
}
