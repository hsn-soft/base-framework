namespace Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;

public sealed class ShipmentDto
{
    public Guid Id { get; init; }

    public Guid OrderId { get; init; }

    public string TrackingNumber { get; init; } = string.Empty;

    public string Carrier { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    /// <summary>Preparing, InTransit, Delivered or Returned.</summary>
    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string? CreatedBy { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? UpdatedBy { get; init; }
}
