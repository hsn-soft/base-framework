using System.ComponentModel.DataAnnotations;
using Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;
using HsnSoft.Base;

namespace Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;

public sealed class ProductStockHistoryCreateDto
{
    [Required]
    [NotDefault]
    public Guid? ProductId { get; set; }

    [Required]
    [StringLength(ProductStockHistoryConsts.ProductNameMaxLength)]
    public string? ProductName { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? OldStock { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? NewStock { get; set; }

    [Required]
    [StringLength(ProductStockHistoryConsts.ReasonMaxLength)]
    public string? Reason { get; set; }

    [NotWhiteSpace]
    [StringLength(ProductStockHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
