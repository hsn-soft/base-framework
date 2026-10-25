using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Filters;

public sealed class GetCustomersPaged : PagedDataRequestDto
{
    /// <summary>A part of the user name.</summary>
    [StringLength(100)]
    public string? UserName { get; set; }

    /// <summary>A part of the full name.</summary>
    [StringLength(100)]
    public string? FullName { get; set; }

    /// <summary>A part of the email.</summary>
    [StringLength(100)]
    public string? Email { get; set; }
}
