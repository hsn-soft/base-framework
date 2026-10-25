using System.ComponentModel.DataAnnotations;
using Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;
using HsnSoft.Base;

namespace Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;

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
