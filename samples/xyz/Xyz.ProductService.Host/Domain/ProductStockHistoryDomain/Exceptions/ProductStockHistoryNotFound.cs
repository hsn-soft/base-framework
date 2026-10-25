using HsnSoft.Base;

namespace Xyz.ProductService.Domain.ProductStockHistoryDomain.Exceptions;

/// <summary>There is no such stock history record: answered as 404.</summary>
public static class ProductStockHistoryNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Stock history record {id} was not found.").WithKey("product_stock_history_not_found", id);
}
