namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos;

public sealed class ProductDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int Stock { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string? CreatedBy { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? UpdatedBy { get; init; }
}
