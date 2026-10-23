using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.EntityFrameworkCore;
using Xyz.ServiceOrder.Host.Domain;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared.Events;

namespace Xyz.ServiceOrder.Host.Handlers;

// The results of the other services come back as events; each handler moves the order one step forward.
// A concurrent update of the same order throws DbUpdateConcurrencyException; the subscriber simply retries the handler.

public sealed class StockReservedHandler(OrderDbContext db, ILogger<StockReservedHandler> logger) : IIntegrationEventHandler<StockReserved>
{
    public async Task HandleAsync(MessageEnvelope<StockReserved> envelope, CancellationToken cancellationToken)
    {
        Order? order = await db.Orders.FirstOrDefaultAsync(x => x.Id == envelope.Message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockReserved for unknown order {OrderId}", envelope.Message.OrderId);
            return;
        }

        order.ApplyStockReserved(envelope.Message.UnitPrice);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StockRejectedHandler(OrderDbContext db, ILogger<StockRejectedHandler> logger) : IIntegrationEventHandler<StockRejected>
{
    public async Task HandleAsync(MessageEnvelope<StockRejected> envelope, CancellationToken cancellationToken)
    {
        Order? order = await db.Orders.FirstOrDefaultAsync(x => x.Id == envelope.Message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockRejected for unknown order {OrderId}", envelope.Message.OrderId);
            return;
        }

        order.ApplyStockRejected(envelope.Message.Reason);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ShipmentDispatchedHandler(OrderDbContext db, ILogger<ShipmentDispatchedHandler> logger) : IIntegrationEventHandler<ShipmentDispatched>
{
    public async Task HandleAsync(MessageEnvelope<ShipmentDispatched> envelope, CancellationToken cancellationToken)
    {
        Order? order = await db.Orders.FirstOrDefaultAsync(x => x.Id == envelope.Message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("ShipmentDispatched for unknown order {OrderId}", envelope.Message.OrderId);
            return;
        }

        order.ApplyShipped(envelope.Message.TrackingNumber);
        await db.SaveChangesAsync(cancellationToken);
    }
}
