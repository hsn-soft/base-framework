namespace Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;

public static class ProductStockHistoryConsts
{
    public const string CollectionName = "product_stock_history";

    public const int ProductNameMaxLength = 128;
    public const int ReasonMaxLength = 32;
    public const int NoteMaxLength = 500;
    public const int ReferenceMaxLength = 96;

    public const string Created = "created";
    public const string Updated = "updated";

    /// <summary>The stock was set aside for an order.</summary>
    public const string Reserved = "reserved";
}
