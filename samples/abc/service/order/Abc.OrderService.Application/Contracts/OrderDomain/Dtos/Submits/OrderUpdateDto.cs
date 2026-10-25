using System.ComponentModel.DataAnnotations;
using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

public sealed class OrderUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>Pending, Confirmed, Shipped, Delivered or Cancelled; a delivered or cancelled order cannot change its status.</summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    [StringLength(OrderConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
