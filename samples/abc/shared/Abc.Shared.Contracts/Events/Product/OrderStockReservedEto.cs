using HsnSoft.Base.Domain.Entities.Events;

namespace Abc.Shared.Contracts.Events.Product;

/// <summary>The stock of every line of an order was set aside, and <paramref name="TotalAmount"/> is what the order costs at the prices of the product service. Published by the product service; the order service confirms the order and the shipment service takes the payment and sends it.</summary>
public sealed record OrderStockReservedEto(Guid OrderId, string CustomerUserName, string ShippingAddress, decimal TotalAmount) : IIntegrationEventMessage;
