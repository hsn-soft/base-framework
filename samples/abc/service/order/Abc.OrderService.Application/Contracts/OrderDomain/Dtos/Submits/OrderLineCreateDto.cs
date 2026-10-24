using System.ComponentModel.DataAnnotations;
using Abc.OrderService.Domain.OrderDomain.Consts;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

public sealed class OrderLineCreateDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(OrderConsts.ProductNameMaxLength)]
    public string ProductName { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; set; }

    [Range(1, 10_000)]
    public int Quantity { get; set; }
}
