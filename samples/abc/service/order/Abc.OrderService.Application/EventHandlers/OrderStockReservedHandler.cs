using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.Shared.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;

namespace Abc.OrderService.Application.EventHandlers;

/// <summary>The product service set the stock of an order aside: the order is confirmed. It runs as the user who made the order, in the tenant of the order (the envelope says who).</summary>
public sealed class OrderStockReservedHandler(IRepository<Order, Guid> orders) : IIntegrationEventHandler<OrderStockReservedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderStockReservedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderStockReservedEto message = envelope.Message;
        Order order = await orders.FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(message.OrderId);
        order.ApplyStockCheck(true, null);
        await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
    }
}
