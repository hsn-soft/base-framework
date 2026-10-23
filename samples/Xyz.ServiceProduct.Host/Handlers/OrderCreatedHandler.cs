using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceProduct.Host.Domain;
using Xyz.ServiceProduct.Host.Endpoints;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared.Events;

namespace Xyz.ServiceProduct.Host.Handlers;

/// <summary>Checks the stock of a new order and answers with StockReserved or StockRejected.</summary>
public sealed class OrderCreatedHandler(ProductStore store, IEventBus eventBus, IDistributedCache cache, ILogger<OrderCreatedHandler> logger)
    : IIntegrationEventHandler<OrderCreated>
{
    public async Task HandleAsync(MessageEnvelope<OrderCreated> envelope, CancellationToken cancellationToken)
    {
        StockReservation reservation = await store.ReserveAsync(envelope.Message.OrderId, envelope.Message.ProductId, envelope.Message.Quantity, cancellationToken);
        await cache.RemoveAsync(ProductEndpoints.CacheKey(envelope.Message.ProductId), cancellationToken);

        switch (reservation.Status)
        {
            case ReservationStatus.Reserved:
                logger.LogInformation("Order {OrderId}: {Quantity} x {ProductId} reserved", envelope.Message.OrderId, envelope.Message.Quantity, envelope.Message.ProductId);
                await eventBus.PublishAsync(new StockReserved(envelope.Message.OrderId, envelope.Message.ProductId, envelope.Message.Quantity, reservation.UnitPrice), cancellationToken: cancellationToken);
                break;

            case ReservationStatus.Rejected:
                logger.LogInformation("Order {OrderId}: stock rejected ({Reason})", envelope.Message.OrderId, reservation.Reason);
                await eventBus.PublishAsync(new StockRejected(envelope.Message.OrderId, reservation.Reason ?? "Rejected."), cancellationToken: cancellationToken);
                break;

            default:
                // Another delivery of the same order is still working on it; let the message be retried.
                throw new InvalidOperationException($"The reservation of order {envelope.Message.OrderId} is still being processed.");
        }
    }
}
