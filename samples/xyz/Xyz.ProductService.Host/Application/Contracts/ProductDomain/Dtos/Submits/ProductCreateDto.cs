using System.ComponentModel.DataAnnotations;
using Xyz.ProductService.Domain.ProductDomain.Consts;

namespace Xyz.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;

public sealed class ProductCreateDto
{
    [Required]
    [StringLength(ProductConsts.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The stock keeping unit; it is unique and cannot be changed later.</summary>
    [Required]
    [StringLength(ProductConsts.SkuMaxLength)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(ProductConsts.DescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? Stock { get; set; }
}
