using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.Shared.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;

namespace Abc.OrderService.Application.EventHandlers;

/// <summary>The product service could not set the stock of an order aside: the order is cancelled, with the reason. It runs as the user who made the order, in the tenant of the order (the envelope says who).</summary>
public sealed class OrderStockRejectedHandler(IRepository<Order, Guid> orders) : IIntegrationEventHandler<OrderStockRejectedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderStockRejectedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderStockRejectedEto message = envelope.Message;
        Order order = await orders.FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(message.OrderId);
        order.ApplyStockCheck(false, message.Reason);
        await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
    }
}
