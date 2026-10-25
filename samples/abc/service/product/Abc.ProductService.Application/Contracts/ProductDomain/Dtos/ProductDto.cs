using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos;

public sealed class ProductDto : AuditedEntityDto<Guid>
{
    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int Stock { get; init; }

    public bool IsActive { get; init; }

    public Guid TenantId { get; init; }

}
