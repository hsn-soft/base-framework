namespace HsnSoft.Base.BlobStoring.Azure;

/// <summary>Connection settings; section <c>AzureBlob</c>. Give either a connection string (Azurite, a development account) or the address of the account, which is reached with the identity of the host.</summary>
public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    /// <summary>The connection string of the storage account (<c>UseDevelopmentStorage=true</c> for Azurite). It holds a key: keep it in the environment, not in a file.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The address of the account, e.g. <c>https://myaccount.blob.core.windows.net</c>. It is reached with the identity of the host (managed identity, <c>az login</c>, environment variables: <c>DefaultAzureCredential</c>), so no key is configured.</summary>
    public string? ServiceUri { get; set; }
}
