using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceProduct.Host.Controllers;
using Xyz.ServiceProduct.Host.Domain;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared;
using Xyz.Shared.Events;

namespace Xyz.ServiceProduct.Host.Handlers;

/// <summary>Checks the stock of a new order and answers with StockReserved or StockRejected.</summary>
public sealed class OrderCreatedHandler(ProductStore store, IEventBus eventBus, IDistributedCache cache, ILogger<OrderCreatedHandler> logger)
    : IIntegrationEventHandler<OrderCreated>
{
    public async Task HandleAsync(MessageEnvelope<OrderCreated> envelope, CancellationToken cancellationToken)
    {
        // a message that lacks what the handler needs is a known error: the message is rejected, not retried
        if (envelope.Message.ProductId == Guid.Empty)
        {
            throw new DomainException($"Order {envelope.Message.OrderId} has no product.");
        }

        // failure demo: a bug nobody expected (an object that is null), to see what the logs say
        if (envelope.Message.Quantity == FailureDemo.HandlerQuantity)
        {
            Dictionary<string, string>? limits = null;
            _ = limits!.Count;
        }

        StockReservation reservation = await store.ReserveAsync(envelope.Message.OrderId, envelope.Message.ProductId, envelope.Message.Quantity, cancellationToken);
        await cache.RemoveAsync(ProductsController.CacheKey(envelope.Message.ProductId), cancellationToken);

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
