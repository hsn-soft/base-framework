using Abc.ProductService.Application.Services;
using Abc.Shared.Contracts.Events.Order;
using Abc.Shared.Contracts.Events.Product;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Abc.ProductService.Http.Host.EventHandlers.Order;

/// <summary>
/// A new order: sets the stock of its lines aside and says what happened, as a fact for whoever cares: <see cref="OrderStockReservedEto"/> (the order is confirmed, the shipment is sent) or <see cref="OrderStockRejectedEto"/> (the order is
/// cancelled). It runs as the user who made the order, in the tenant of the order (the envelope says who), so the products it reads are the tenant's and the stock history it writes has the user.
/// </summary>
public sealed class OrderCreatedHandler(StockReservationService reservations, IEventBus eventBus) : IIntegrationEventHandler<OrderCreatedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderCreatedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderCreatedEto order = envelope.Message;
        StockReservation reservation = await reservations.ReserveAsync(order.OrderId, order.Lines, cancellationToken).ConfigureAwait(false);
        if (reservation.IsReserved)
        {
            await eventBus.PublishAsync(new OrderStockReservedEto(order.OrderId, order.CustomerUserName, order.ShippingAddress, reservation.TotalAmount), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await eventBus.PublishAsync(new OrderStockRejectedEto(order.OrderId, reservation.Reason ?? "The stock could not be set aside."), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
