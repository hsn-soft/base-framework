namespace Edx.Integration.FirmInvoice.MongoDB;

/// <summary>Where the firm invoice integration keeps its data: the prefix of its collections and the name of its connection string. They are in the code of the service, not in its settings.</summary>
public static class MongoFirmInvoiceDbProperties
{
    public static string CollectionPrefix { get; set; } = string.Empty;

    public const string ConnectionStringName = "Mongo-FirmInvoiceService";
}
