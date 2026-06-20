using Microsoft.Extensions.Logging;
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;

namespace Hhs.VideoGeneratorService.Providers.Storage;

/// <summary>
/// Factory for creating ICdnStorageProvider instances based on CDN provider configuration.
/// </summary>
public sealed class CdnStorageProviderFactory
{
    private readonly ICdnProviderResolver _cdnProviderResolver;
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public CdnStorageProviderFactory(
        ICdnProviderResolver cdnProviderResolver,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        _cdnProviderResolver = cdnProviderResolver;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an ICdnStorageProvider instance for the given CDN provider key.
    /// </summary>
    /// <param name="cdnProviderKey">CDN provider key (e.g., "LocalStorageCdn", "S3Cdn", "AzureCdn")</param>
    /// <returns>ICdnStorageProvider instance</returns>
    /// <exception cref="InvalidOperationException">If provider type is not supported</exception>
    public ICdnStorageProvider CreateProvider(string cdnProviderKey)
    {
        if (string.IsNullOrWhiteSpace(cdnProviderKey))
            throw new ArgumentException("CDN provider key cannot be null or empty.", nameof(cdnProviderKey));

        var cdnSettings = _cdnProviderResolver.Resolve(cdnProviderKey);
        var storageSettings = cdnSettings.Storage;
        var logger = _loggerFactory.CreateLogger(typeof(LocalStorageProvider));

        return storageSettings.Type?.ToLowerInvariant() switch
        {
            "local" => new LocalStorageProvider(
                cdnSettings,
                storageSettings as LocalStorageSettings ?? throw new InvalidOperationException("Expected LocalStorageSettings"),
                logger as dynamic ?? _loggerFactory.CreateLogger<LocalStorageProvider>()),

            "s3" => new S3StorageProvider(
                cdnSettings,
                storageSettings as S3StorageSettings ?? throw new InvalidOperationException("Expected S3StorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<S3StorageProvider>()),

            "azure" => new AzureStorageProvider(
                cdnSettings,
                storageSettings as AzureBlobStorageSettings ?? throw new InvalidOperationException("Expected AzureBlobStorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<AzureStorageProvider>()),

            "cloudflarer2" => new CloudflareR2StorageProvider(
                cdnSettings,
                storageSettings as CloudflareR2StorageSettings ?? throw new InvalidOperationException("Expected CloudflareR2StorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<CloudflareR2StorageProvider>()),

            _ => throw new InvalidOperationException(
                $"Unsupported storage type '{storageSettings.Type}' for CDN provider '{cdnProviderKey}'. " +
                $"Supported types: local, s3, azure, cloudflarer2")
        };
    }
}
