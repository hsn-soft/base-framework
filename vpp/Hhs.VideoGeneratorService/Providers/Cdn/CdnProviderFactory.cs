using Hhs.Shared.Configuration.Providers;
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
    /// Supports S3 (CdnBunnyS3) and HttpCdn (CdnLocalMinio, CdnBunnySelf, CdnAbc).
    /// </summary>
    /// <param name="cdnProviderKey">CDN provider key (e.g., "CdnLocalMinio", "CdnBunnyS3", "CdnAbc")</param>
    /// <returns>ICdnProvider instance based on storage Type (s3 → CdnS3Provider, httpcnd → CdnHttpProvider)</returns>
    /// <exception cref="InvalidOperationException">If provider type is not supported</exception>
    public ICdnProvider CreateProvider(string cdnProviderKey)
    {
        if (string.IsNullOrWhiteSpace(cdnProviderKey))
            throw new ArgumentException("CDN provider key cannot be null or empty.", nameof(cdnProviderKey));

        var cdnSettings = _cdnProviderResolver.Resolve(cdnProviderKey);
        var storageSettings = cdnSettings.Storage as StorageProviderSettingsBase ?? throw new InvalidOperationException($"Storage settings not found for CDN provider '{cdnProviderKey}'");
        var logger = _loggerFactory.CreateLogger(typeof(CdnS3Provider));

        return storageSettings.Type?.ToLowerInvariant() switch
        {
            "s3" => new CdnS3Provider(
                cdnSettings,
                storageSettings as S3StorageSettings ?? throw new InvalidOperationException("Expected S3StorageSettings"),
                _httpClient,
                _loggerFactory.CreateLogger<CdnS3Provider>()),

            "httpcnd" => new CdnHttpProvider(
                cdnSettings,
                _httpClient,
                _loggerFactory.CreateLogger<CdnHttpProvider>()),

            _ => throw new InvalidOperationException(
                $"Unsupported storage type '{storageSettings.Type}' for CDN provider '{cdnProviderKey}'. " +
                $"Supported types: s3, httpcnd")
        };
    }
}
