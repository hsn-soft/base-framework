using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Storage;
using Hhs.Shared.Providers;

namespace Hhs.Shared.Providers.Storage;

public interface ICdnProviderResolver
{
    CdnProviderSettingsBase Resolve(string cdnProviderKey);
}

public sealed class CdnProviderResolver : ICdnProviderResolver
{
    private readonly Dictionary<string, CdnProviderSettingsBase> _providers;

    public CdnProviderResolver(Dictionary<string, CdnProviderSettingsBase> providers)
    {
        _providers = providers ?? new Dictionary<string, CdnProviderSettingsBase>(StringComparer.OrdinalIgnoreCase);
    }

    public CdnProviderSettingsBase Resolve(string cdnProviderKey)
    {
        if (string.IsNullOrWhiteSpace(cdnProviderKey))
            throw new ArgumentException("CDN provider key cannot be null or empty.", nameof(cdnProviderKey));

        if (_providers.TryGetValue(cdnProviderKey, out var provider))
            return provider;

        throw new InvalidOperationException($"CDN provider '{cdnProviderKey}' not found in configuration. Available providers: {string.Join(", ", _providers.Keys)}");
    }
}
