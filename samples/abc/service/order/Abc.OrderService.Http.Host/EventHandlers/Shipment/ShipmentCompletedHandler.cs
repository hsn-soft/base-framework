using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.Shared.Contracts.Events.Shipment;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;

namespace Abc.OrderService.Http.Host.EventHandlers.Shipment;

/// <summary>The shipment service says how the delivery of an order ended: the order is delivered, or cancelled with the reason. It runs as the user who made the order.</summary>
public sealed class ShipmentCompletedHandler(IRepository<Order, Guid> orders) : IIntegrationEventHandler<ShipmentCompletedEto>
{
    public async Task HandleAsync(MessageEnvelope<ShipmentCompletedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ShipmentCompletedEto message = envelope.Message;
        Order order = await orders.FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(message.OrderId);
        order.ApplyDelivery(message.IsSuccessful, message.Reason);
        await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
    }
}
