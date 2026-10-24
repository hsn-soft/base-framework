using HsnSoft.Base;

namespace Abc.ProductService.Domain.ProductDomain.Exceptions;

/// <summary>The sku is taken (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class ProductSkuDuplicateException(string sku) : BusinessException(Code, $"The sku '{sku}' is taken.")
{
    public const string Code = "product_sku_duplicate";
}
