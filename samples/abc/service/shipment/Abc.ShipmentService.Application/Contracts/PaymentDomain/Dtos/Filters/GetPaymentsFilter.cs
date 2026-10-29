using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Filters;

public sealed class GetPaymentsFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>Only the payment of this order.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Only the payments of this customer.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Pending, Succeeded, Failed or Refunded.</summary>
    [StringLength(20)]
    public string? Status { get; set; }
}
