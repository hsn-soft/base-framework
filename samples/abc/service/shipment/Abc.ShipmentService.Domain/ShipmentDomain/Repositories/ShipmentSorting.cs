using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Repositories;

/// <summary>The fields a list of shipments may be sorted by (the names a client sends, <c>"carrier, createdAt desc"</c>) and the sorting when it sends none: the newest first.</summary>
public static class ShipmentSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<Shipment> Fields = new SortFields<Shipment>()
        .Add("createdAt", shipment => shipment.CreatedAt)
        .Add("trackingNumber", shipment => shipment.TrackingNumber)
        .Add("carrier", shipment => shipment.NormalizedCarrier)
        .Add("status", shipment => shipment.Status);
}
