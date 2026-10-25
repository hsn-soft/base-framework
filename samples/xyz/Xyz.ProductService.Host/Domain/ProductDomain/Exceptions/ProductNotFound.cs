using HsnSoft.Base;

namespace Xyz.ProductService.Domain.ProductDomain.Exceptions;

/// <summary>There is no such product: answered as 404.</summary>
public static class ProductNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Product {id} was not found.").WithKey("product_not_found", id);
}
