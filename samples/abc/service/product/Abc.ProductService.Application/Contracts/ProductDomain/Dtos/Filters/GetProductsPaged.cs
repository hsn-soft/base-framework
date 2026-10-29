using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Filters;

public sealed class GetProductsPaged : PagedDataRequestDto
{
    /// <summary>A part of the name.</summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>A part of the sku.</summary>
    [StringLength(100)]
    public string? Sku { get; set; }

    /// <summary>Only the active (<see langword="true"/>) or only the inactive (<see langword="false"/>) products; all when it is not given.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Only the products in stock (<see langword="true"/>) or only the ones out of stock (<see langword="false"/>); all when it is not given.</summary>
    public bool? InStock { get; set; }
}
