using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Filters;

public sealed class GetOrdersPaged : PagedDataRequestDto
{
    /// <summary>A part of the customer's user name.</summary>
    [StringLength(100)]
    public string? CustomerUserName { get; set; }

    /// <summary>Only the orders in this status (Pending, Confirmed, Shipped, Delivered, Cancelled); all when it is not given.</summary>
    [StringLength(20)]
    public string? Status { get; set; }
}
