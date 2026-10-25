using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;

public sealed class ShipmentDto : AuditedEntityDto<Guid>
{
    public Guid OrderId { get; init; }

    public string TrackingNumber { get; init; } = string.Empty;

    public string Carrier { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    /// <summary>Preparing, InTransit, Delivered or Returned.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The status in the language of the request (translated by the application service).</summary>
    public string StatusText { get; init; } = string.Empty;

    public Guid TenantId { get; init; }

}
