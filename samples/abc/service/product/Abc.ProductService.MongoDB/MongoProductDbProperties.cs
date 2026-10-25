namespace Abc.ProductService.MongoDB;

/// <summary>Where the product service keeps its data: the prefix of its collections and the name of its connection string. They are in the code of the service, not in its settings.</summary>
public static class MongoProductDbProperties
{
    public static string CollectionPrefix { get; set; } = string.Empty;

    public const string ConnectionStringName = "Mongo-ProductService";
}
