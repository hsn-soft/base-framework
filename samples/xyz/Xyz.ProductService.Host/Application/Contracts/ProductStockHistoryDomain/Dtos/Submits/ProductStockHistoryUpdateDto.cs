using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Consts;

namespace Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;

/// <summary>What can be corrected of a record: its reason and its note (what the stock was and became is a fact).</summary>
public sealed class ProductStockHistoryUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(ProductStockHistoryConsts.ReasonMaxLength)]
    public string? Reason { get; set; }

    [NotWhiteSpace]
    [StringLength(ProductStockHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
