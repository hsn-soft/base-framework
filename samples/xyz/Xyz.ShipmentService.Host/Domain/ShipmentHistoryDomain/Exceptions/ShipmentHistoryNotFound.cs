using HsnSoft.Base;

namespace Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Exceptions;

/// <summary>There is no such shipment history record: answered as 404.</summary>
public static class ShipmentHistoryNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Shipment history record {id} was not found.").WithKey("shipment_history_not_found", id);
}
