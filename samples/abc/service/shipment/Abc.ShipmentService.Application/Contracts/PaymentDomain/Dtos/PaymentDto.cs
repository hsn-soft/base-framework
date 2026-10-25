using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;

public sealed class PaymentDto : AuditedEntityDto<Guid>
{
    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>Pending, Succeeded, Failed or Refunded.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The status in the language of the request (translated by the application service).</summary>
    public string StatusText { get; init; } = string.Empty;

    public string? Reference { get; init; }

    public string? FailureReason { get; init; }

    public Guid TenantId { get; init; }
}
