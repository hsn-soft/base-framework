namespace Edx.Integration.GibInvoice.MongoDB;

/// <summary>Where the integration service keeps its data: the prefix of its collections and the name of its connection string. They are in the code of the service, not in its settings.</summary>
public static class MongoIntegrationDbProperties
{
    public const string ConnectionStringName = "Mongo-IntegrationService";

    public const string ExchangeUserCollectionName = "ExchangeUser";

    public static string CollectionPrefix { get; set; } = string.Empty;
}
