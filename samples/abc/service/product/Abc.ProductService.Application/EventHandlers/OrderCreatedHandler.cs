using Abc.ProductService.Application.Services;
using Abc.Shared.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;

namespace Abc.ProductService.Application.EventHandlers;

/// <summary>
/// A new order: checks its stock in the tenant of the order and tells the order service the answer (<see cref="OrderStockCheckedEto"/>); when the stock was set aside it also tells the shipment service to send the parcel
/// (<see cref="ShipmentRequestedEto"/>).
/// </summary>
public sealed class OrderCreatedHandler(StockReservationService reservations, IEventBus eventBus, ICurrentTenant tenant) : IIntegrationEventHandler<OrderCreatedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderCreatedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderCreatedEto order = envelope.Message;
        StockReservation reservation;
        using (tenant.Change(order.TenantId))
        {
            reservation = await reservations.ReserveAsync(order.OrderId, order.Lines, cancellationToken).ConfigureAwait(false);
        }

        await eventBus.PublishAsync(new OrderStockCheckedEto(order.OrderId, order.TenantId, reservation.IsReserved, reservation.Reason), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (reservation.IsReserved)
        {
            await eventBus.PublishAsync(new ShipmentRequestedEto(order.OrderId, order.TenantId, order.ShippingAddress), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
