using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Filters;

public sealed class GetProductStockHistoriesFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>Only the history of this product.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>A part of the product name.</summary>
    [StringLength(100)]
    public string? ProductName { get; set; }

    /// <summary>Why the stock changed (<c>created</c>, <c>updated</c>, ...); the exact word.</summary>
    [StringLength(32)]
    public string? Reason { get; set; }
}
