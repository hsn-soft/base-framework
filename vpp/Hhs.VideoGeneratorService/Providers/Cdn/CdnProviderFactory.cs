using Microsoft.Extensions.Logging;
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// Factory for creating ICdnProvider instances based on CDN provider configuration.
/// </summary>
public sealed class CdnProviderFactory
{
    private readonly ICdnProviderResolver _cdnProviderResolver;
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public CdnProviderFactory(
        ICdnProviderResolver cdnProviderResolver,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        _cdnProviderResolver = cdnProviderResolver;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an ICdnProvider instance for the given CDN provider key.
    /// </summary>
    /// <param name="cdnProviderKey">CDN provider key (e.g., "LocalStorageCdn", "S3Cdn", "AzureCdn")</param>
    /// <returns>ICdnProvider instance</returns>
    /// <exception cref="InvalidOperationException">If provider type is not supported</exception>
    public ICdnProvider CreateProvider(string cdnProviderKey)
    {
        if (string.IsNullOrWhiteSpace(cdnProviderKey))
            throw new ArgumentException("CDN provider key cannot be null or empty.", nameof(cdnProviderKey));

        var cdnSettings = _cdnProviderResolver.Resolve(cdnProviderKey);
        var storageSettings = cdnSettings.Storage;
        var logger = _loggerFactory.CreateLogger(typeof(LocalCdnProvider));

        return storageSettings.Type?.ToLowerInvariant() switch
        {
            "local" => new LocalCdnProvider(
                cdnSettings,
                storageSettings as LocalStorageSettings ?? throw new InvalidOperationException("Expected LocalStorageSettings"),
                logger as dynamic ?? _loggerFactory.CreateLogger<LocalCdnProvider>()),

            "s3" => new S3CdnProvider(
                cdnSettings,
                storageSettings as S3StorageSettings ?? throw new InvalidOperationException("Expected S3StorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<S3CdnProvider>()),

            "azure" => new AzureCdnProvider(
                cdnSettings,
                storageSettings as AzureBlobStorageSettings ?? throw new InvalidOperationException("Expected AzureBlobStorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<AzureCdnProvider>()),

            "cloudflarer2" => new CloudflareR2CdnProvider(
                cdnSettings,
                storageSettings as CloudflareR2StorageSettings ?? throw new InvalidOperationException("Expected CloudflareR2StorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<CloudflareR2CdnProvider>()),

            _ => throw new InvalidOperationException(
                $"Unsupported storage type '{storageSettings.Type}' for CDN provider '{cdnProviderKey}'. " +
                $"Supported types: local, s3, azure, cloudflarer2")
        };
    }
}
