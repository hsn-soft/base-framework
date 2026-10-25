namespace Abc.ProductService.MongoDB;

/// <summary>Where the stock history of the product service is kept: another MongoDB than the one of the products (its own connection string), under its own key in the container. They are in the code of the service, not in its settings.</summary>
public static class MongoStockDbProperties
{
    public static string CollectionPrefix { get; set; } = string.Empty;

    public const string ConnectionStringName = "Mongo-StockService";

    /// <summary>The key the stock database is registered under: the products' is the one without a key.</summary>
    public const string ServiceKey = "stock";
}
