using HsnSoft.Base;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Exceptions;

/// <summary>There is no such shipment: answered as 404.</summary>
public static class ShipmentNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Shipment {id} was not found.").WithKey("shipment_not_found", id);
}
