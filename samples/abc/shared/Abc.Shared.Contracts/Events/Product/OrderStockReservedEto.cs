using HsnSoft.Base.Domain.Entities.Events;

namespace Abc.Shared.Contracts.Events.Product;

/// <summary>The stock of every line of an order was set aside. Published by the product service; the order service confirms the order and the shipment service sends it.</summary>
public sealed record OrderStockReservedEto(Guid OrderId, string ShippingAddress) : IIntegrationEventMessage;
