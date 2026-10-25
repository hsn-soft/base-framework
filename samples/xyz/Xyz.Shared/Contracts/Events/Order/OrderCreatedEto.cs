using HsnSoft.Base.Domain.Entities.Events;

namespace Xyz.Shared.Contracts.Events.Order;

/// <summary>An order was made. Published by the order service; the product service sets the stock of its lines aside. Who made it (the user, the tenant) is in the envelope.</summary>
public sealed record OrderCreatedEto(Guid OrderId, string CustomerUserName, string ShippingAddress, IReadOnlyList<OrderLineEto> Lines) : IIntegrationEventMessage;

/// <summary>One line of an order, as the product service needs it.</summary>
public sealed record OrderLineEto(Guid ProductId, int Quantity);
