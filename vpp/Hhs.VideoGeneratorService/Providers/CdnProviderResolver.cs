using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;
using Hhs.VideoGeneratorService.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Providers;

public interface ICdnProviderResolver
{
    ICdnProvider Resolve(string cdnProviderKey);
}

public sealed class CdnProviderResolver : ICdnProviderResolver
{
    private readonly Dictionary<string, CdnProviderSettingsBase> _providers;
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public CdnProviderResolver(
        Dictionary<string, CdnProviderSettingsBase> providers,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        _providers = providers ?? new Dictionary<string, CdnProviderSettingsBase>(StringComparer.OrdinalIgnoreCase);
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public ICdnProvider Resolve(string cdnProviderKey)
    {
        if (string.IsNullOrWhiteSpace(cdnProviderKey))
            throw new ArgumentException("CDN provider key cannot be null or empty.", nameof(cdnProviderKey));

        if (!_providers.TryGetValue(cdnProviderKey, out var cdnSettings))
            throw new InvalidOperationException($"CDN provider '{cdnProviderKey}' not found in configuration. Available providers: {string.Join(", ", _providers.Keys)}");

        var storageSettings = cdnSettings.Storage as StorageProviderSettingsBase
            ?? throw new InvalidOperationException($"Storage settings not found for CDN provider '{cdnProviderKey}'");

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
