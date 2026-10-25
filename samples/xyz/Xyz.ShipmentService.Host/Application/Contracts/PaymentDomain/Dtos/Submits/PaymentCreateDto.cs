using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.PaymentDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Submits;

/// <summary>A payment made by hand: it is pending until it is settled (<c>PUT</c>). The payments of orders are made and settled by the service when the stock of the order was set aside.</summary>
public sealed class PaymentCreateDto
{
    [Required]
    [NotDefault]
    public Guid? OrderId { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerId { get; set; }

    [Required]
    [Range(0.01, 1_000_000)]
    public decimal? Amount { get; set; }

    [StringLength(PaymentConsts.CurrencyLength, MinimumLength = PaymentConsts.CurrencyLength)]
    public string? Currency { get; set; }
}
