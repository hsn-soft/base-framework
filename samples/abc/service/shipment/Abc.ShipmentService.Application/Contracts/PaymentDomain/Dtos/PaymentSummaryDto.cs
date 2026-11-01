namespace Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;

/// <summary>How many payments there are in each status, at one moment: what the scheduled job of the shipment service looks at.</summary>
public sealed class PaymentSummaryDto
{
    public long Pending { get; init; }

    public long Succeeded { get; init; }

    public long Failed { get; init; }

    public long Refunded { get; init; }

    public DateTimeOffset At { get; init; }
}
