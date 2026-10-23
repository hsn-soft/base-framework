using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Xyz.ServiceShipment.Host.Domain;
using Xyz.ServiceShipment.Host.Persistence;
using Xyz.Shared.Events;

namespace Xyz.ServiceShipment.Host.Handlers;

/// <summary>Once the stock is reserved the goods are dispatched, and the order is told with ShipmentDispatched.</summary>
public sealed class StockReservedHandler(ShipmentStore store, IEventBus eventBus, ILogger<StockReservedHandler> logger)
    : IIntegrationEventHandler<StockReserved>
{
    public async Task HandleAsync(MessageEnvelope<StockReserved> envelope, CancellationToken cancellationToken)
    {
        Shipment shipment = await store.DispatchAsync(envelope.Message.OrderId, envelope.Message.ProductId, envelope.Message.Quantity, cancellationToken);
        logger.LogInformation("Order {OrderId}: dispatched as {TrackingNumber}", envelope.Message.OrderId, shipment.TrackingNumber);

        await eventBus.PublishAsync(new ShipmentDispatched(envelope.Message.OrderId, shipment.Id, shipment.TrackingNumber), cancellationToken: cancellationToken);
    }
}
