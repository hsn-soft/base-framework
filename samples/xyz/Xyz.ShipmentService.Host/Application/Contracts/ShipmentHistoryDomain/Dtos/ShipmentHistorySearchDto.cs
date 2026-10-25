using HsnSoft.Base.Application.Dtos;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos;

/// <summary>What a search shows of a shipment history record: enough to recognize and pick one.</summary>
public sealed class ShipmentHistorySearchDto : EntityDto<Guid>
{
    public string TrackingNumber { get; init; } = string.Empty;

    public string Outcome { get; init; } = string.Empty;
}
