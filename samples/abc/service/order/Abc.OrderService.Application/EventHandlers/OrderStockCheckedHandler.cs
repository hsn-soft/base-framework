using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.Shared.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;

namespace Abc.OrderService.Application.EventHandlers;

/// <summary>The product service checked the stock of an order: the order is confirmed, or cancelled with the reason. It acts in the tenant of the order (a message has no request, so no tenant of its own).</summary>
public sealed class OrderStockCheckedHandler(IRepository<Order, Guid> orders, ICurrentTenant tenant) : IIntegrationEventHandler<OrderStockCheckedEto>
{
    public async Task HandleAsync(MessageEnvelope<OrderStockCheckedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderStockCheckedEto message = envelope.Message;
        using (tenant.Change(message.TenantId))
        {
            Order order = await orders.FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(message.OrderId);
            order.ApplyStockCheck(message.IsAvailable, message.Reason);
            await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        }
    }
}
