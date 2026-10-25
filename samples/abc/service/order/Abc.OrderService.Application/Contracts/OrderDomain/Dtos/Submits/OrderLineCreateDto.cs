using System.ComponentModel.DataAnnotations;
using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

public sealed class OrderLineCreateDto
{
    [Required]
    [NotDefault]
    public Guid? ProductId { get; set; }

    [Required]
    [StringLength(OrderConsts.ProductNameMaxLength)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Range(0, 1_000_000)]
    public decimal? UnitPrice { get; set; }

    [Required]
    [Range(1, 10_000)]
    public int? Quantity { get; set; }
}
