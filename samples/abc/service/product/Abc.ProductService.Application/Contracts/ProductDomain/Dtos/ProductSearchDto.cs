namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos;

public sealed class ProductSearchDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;
}
