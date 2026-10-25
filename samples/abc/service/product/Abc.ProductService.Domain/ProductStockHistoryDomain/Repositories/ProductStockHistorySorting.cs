using Abc.ProductService.Domain.ProductStockHistoryDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Abc.ProductService.Domain.ProductStockHistoryDomain.Repositories;

/// <summary>The fields a list of the stock history may be sorted by (<c>"createdAt desc, change"</c>) and the sorting when it sends none: the latest first.</summary>
public static class ProductStockHistorySorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<ProductStockHistory> Fields = new SortFields<ProductStockHistory>()
        .Add("productName", history => history.NormalizedProductName)
        .Add("reason", history => history.Reason)
        .Add("change", history => history.Change)
        .Add("createdAt", history => history.CreatedAt);
}
