using System.ComponentModel.DataAnnotations;
using Abc.OrderService.Domain.OrderDomain.Consts;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;

public sealed class OrderCreateDto
{
    [StringLength(OrderConsts.NoteMaxLength)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<OrderLineCreateDto> Lines { get; set; } = [];
}
