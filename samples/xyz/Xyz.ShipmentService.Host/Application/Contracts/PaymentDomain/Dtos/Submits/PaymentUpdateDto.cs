using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.PaymentDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Submits;

/// <summary>Corrects the status of a payment (a pending one is settled or fails, a succeeded one is refunded; a failed or refunded one is final).</summary>
public sealed class PaymentUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>Pending, Succeeded, Failed or Refunded.</summary>
    [Required]
    [StringLength(20)]
    public string? Status { get; set; }

    /// <summary>Why it failed (required for Failed); the reference of the provider for Succeeded.</summary>
    [StringLength(PaymentConsts.ReasonMaxLength)]
    public string? Reason { get; set; }
}
