using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// Factory for creating ICdnProvider instances based on storage type.
/// Settings are resolved by CdnProviderResolver; factory only creates providers.
/// </summary>
public sealed class CdnProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public CdnProviderFactory(
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an ICdnProvider instance based on storage type in settings.
    /// </summary>
    /// <param name="cdnProviderKey">Provider key (used for identification)</param>
    /// <param name="cdnSettings">CDN provider settings from configuration</param>
    /// <returns>ICdnProvider instance (CdnS3Provider or CdnHttpProvider)</returns>
    /// <exception cref="InvalidOperationException">If storage type is not supported</exception>
    public ICdnProvider CreateProvider(string cdnProviderKey, CdnProviderSettingsBase cdnSettings)
    {
        var storageSettings = cdnSettings.Storage as StorageProviderSettingsBase 
            ?? throw new InvalidOperationException($"Storage settings not found for CDN provider '{cdnProviderKey}'");

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
