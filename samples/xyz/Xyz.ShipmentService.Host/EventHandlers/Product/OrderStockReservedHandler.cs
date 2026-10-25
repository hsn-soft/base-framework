using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Xyz.Shared.Contracts.Events.Product;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Services;
using Xyz.ShipmentService.Application.Contracts.Events;
using Xyz.ShipmentService.Application.Services;

namespace Xyz.ShipmentService.EventHandlers.Product;

/// <summary>
/// The stock of an order was set aside (the product service published it for whoever cares): the customer of the order is made when the service does not know the user yet, the payment is taken, and the service tells itself how it went
/// (<see cref="PaymentProcessedEto"/>, an internal event) so the next step, sending the parcel, is a step of its own. It runs as the user who made the order, in the tenant of the order (the envelope says who).
/// </summary>
public sealed class OrderStockReservedHandler(ICustomerAppService customers, PaymentProcessor payments, IEventBus eventBus) : IIntegrationEventHandler<OrderStockReservedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderStockReservedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderStockReservedEto order = envelope.Message;
        CustomerDto customer = await customers.EnsureAsync(order.CustomerUserName, order.ShippingAddress, cancellationToken).ConfigureAwait(false);
        PaymentResult result = await payments.ProcessAsync(order.OrderId, customer.Id, order.TotalAmount, order.ShippingAddress, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new PaymentProcessedEto(order.OrderId, result.PaymentId, result.IsSuccessful, result.Reason, order.ShippingAddress), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
