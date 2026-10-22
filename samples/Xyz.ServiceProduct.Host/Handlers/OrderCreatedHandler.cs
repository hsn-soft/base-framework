using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceProduct.Host.Domain;
using Xyz.ServiceProduct.Host.Endpoints;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared.Events;
using HsnSoft.Base.EventBus;

namespace Xyz.ServiceProduct.Host.Handlers;

/// <summary>Checks the stock of a new order and answers with StockReserved or StockRejected.</summary>
public sealed class OrderCreatedHandler(ProductStore store, IEventBus eventBus, IDistributedCache cache, ILogger<OrderCreatedHandler> logger)
    : IIntegrationEventHandler<OrderCreated>
{
    public async Task HandleAsync(OrderCreated @event, CancellationToken cancellationToken)
    {
        var reservation = await store.ReserveAsync(@event.OrderId, @event.ProductId, @event.Quantity, cancellationToken);
        await cache.RemoveAsync(ProductEndpoints.CacheKey(@event.ProductId), cancellationToken);

        switch (reservation.Status)
        {
            case ReservationStatus.Reserved:
                logger.LogInformation("Order {OrderId}: {Quantity} x {ProductId} reserved", @event.OrderId, @event.Quantity, @event.ProductId);
                await eventBus.PublishAsync(new StockReserved(@event.OrderId, @event.ProductId, @event.Quantity, reservation.UnitPrice), cancellationToken);
                break;

            case ReservationStatus.Rejected:
                logger.LogInformation("Order {OrderId}: stock rejected ({Reason})", @event.OrderId, reservation.Reason);
                await eventBus.PublishAsync(new StockRejected(@event.OrderId, reservation.Reason ?? "Rejected."), cancellationToken);
                break;

            default:
                // Another delivery of the same order is still working on it; let the message be retried.
                throw new InvalidOperationException($"The reservation of order {@event.OrderId} is still being processed.");
        }
    }
}
