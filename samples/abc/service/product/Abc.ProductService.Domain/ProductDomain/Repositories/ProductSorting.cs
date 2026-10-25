using Abc.ProductService.Domain.ProductDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Abc.ProductService.Domain.ProductDomain.Repositories;

/// <summary>The fields a list of products may be sorted by (the names a client sends, <c>"price desc, name"</c>) and the sorting when it sends none.</summary>
public static class ProductSorting
{
    public const string Default = "name asc";

    public static readonly SortFields<Product> Fields = new SortFields<Product>()
        .Add("name", product => product.NormalizedName)
        .Add("sku", product => product.Sku)
        .Add("price", product => product.Price)
        .Add("stock", product => product.Stock)
        .Add("isActive", product => product.IsActive)
        .Add("createdAt", product => product.CreatedAt);
}
