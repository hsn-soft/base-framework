using System.ComponentModel.DataAnnotations;
using Abc.ProductService.Domain.ProductDomain.Consts;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;

public sealed class ProductUpdateDto
{
    [Required]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(ProductConsts.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(ProductConsts.DescriptionMaxLength)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? Stock { get; set; }

    [Required]
    public bool? IsActive { get; set; }
}
