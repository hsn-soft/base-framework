using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos;

public sealed class ShipmentHistoryDto : AuditedEntityDto<Guid>
{
    public Guid ShipmentId { get; init; }

    public Guid OrderId { get; init; }

    public string TrackingNumber { get; init; } = string.Empty;

    public string Outcome { get; init; } = string.Empty;

    public string? Note { get; init; }
}
