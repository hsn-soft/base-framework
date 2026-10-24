using HsnSoft.Base;

namespace Abc.ProductService.Domain.ProductDomain.Exceptions;

/// <summary>There is no such product: answered as 404.</summary>
public static class ProductNotFound
{
    public static NotFoundException For(Guid id) => new($"Product {id} was not found.");
}
