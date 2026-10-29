using System.ComponentModel.DataAnnotations;
using Abc.OrderService.Domain.OrderDomain.Consts;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

public sealed class OrderCreateDto
{
    /// <summary>Where the parcel goes (the shipment service sends it there once the stock is set aside).</summary>
    [Required]
    [StringLength(OrderConsts.ShippingAddressMaxLength)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(OrderConsts.NoteMaxLength)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<OrderLineCreateDto> Lines { get; set; } = [];
}
