using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos;

public sealed class ProductSearchDto : EntityDto<Guid>
{
    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;
}
