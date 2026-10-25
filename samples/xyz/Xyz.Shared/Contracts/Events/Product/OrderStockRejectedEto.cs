using HsnSoft.Base.Domain.Entities.Events;

namespace Xyz.Shared.Contracts.Events.Product;

/// <summary>The stock of an order could not be set aside: nothing was taken, and <paramref name="Reason"/> says why. Published by the product service; the order service cancels the order.</summary>
public sealed record OrderStockRejectedEto(Guid OrderId, string Reason) : IIntegrationEventMessage;
