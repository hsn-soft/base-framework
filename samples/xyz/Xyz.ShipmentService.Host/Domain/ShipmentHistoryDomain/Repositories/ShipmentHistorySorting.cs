using HsnSoft.Base.Domain.Sorting;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Entities;

namespace Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Repositories;

/// <summary>The fields a list of the shipment history may be sorted by (<c>"createdAt desc, outcome"</c>) and the sorting when it sends none: the latest first.</summary>
public static class ShipmentHistorySorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<ShipmentHistory> Fields = new SortFields<ShipmentHistory>()
        .Add("trackingNumber", history => history.TrackingNumber)
        .Add("outcome", history => history.Outcome)
        .Add("createdAt", history => history.CreatedAt);
}
