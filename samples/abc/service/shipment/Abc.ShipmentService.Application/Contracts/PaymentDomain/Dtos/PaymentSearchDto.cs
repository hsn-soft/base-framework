using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;

/// <summary>What a search shows of a payment: enough to recognize and pick one.</summary>
public sealed class PaymentSearchDto : EntityDto<Guid>
{
    public Guid OrderId { get; init; }

    public decimal Amount { get; init; }

    public string Status { get; init; } = string.Empty;
}
