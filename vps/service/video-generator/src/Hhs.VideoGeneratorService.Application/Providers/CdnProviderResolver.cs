using Hhs.VideoGeneratorService.Application.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Application.Providers;

public interface ICdnProviderResolver
{
    ICdnProvider Resolve(string cdnProviderKey);
}

public sealed class CdnProviderResolver(IEnumerable<ICdnProvider> providers) : ICdnProviderResolver
{
    public ICdnProvider Resolve(string cdnProviderKey) =>
        providers.FirstOrDefault(x => x.ProviderKey.Equals(cdnProviderKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"CDN provider not found: {cdnProviderKey}");
}
