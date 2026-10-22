using Xyz.ServiceShipment.Host.Persistence;
using Xyz.Shared.Events;
using HsnSoft.Base.EventBus;

namespace Xyz.ServiceShipment.Host.Handlers;

/// <summary>Once the stock is reserved the goods are dispatched, and the order is told with ShipmentDispatched.</summary>
public sealed class StockReservedHandler(ShipmentStore store, IEventBus eventBus, ILogger<StockReservedHandler> logger)
    : IIntegrationEventHandler<StockReserved>
{
    public async Task HandleAsync(StockReserved @event, CancellationToken cancellationToken)
    {
        var shipment = await store.DispatchAsync(@event.OrderId, @event.ProductId, @event.Quantity, cancellationToken);
        logger.LogInformation("Order {OrderId}: dispatched as {TrackingNumber}", @event.OrderId, shipment.TrackingNumber);

        await eventBus.PublishAsync(new ShipmentDispatched(@event.OrderId, shipment.Id, shipment.TrackingNumber), cancellationToken);
    }
}
