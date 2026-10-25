using HsnSoft.Base.Application.Dtos;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;

public sealed class ShipmentSearchDto : EntityDto<Guid>
{
    public string TrackingNumber { get; init; } = string.Empty;

    public string Carrier { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}
