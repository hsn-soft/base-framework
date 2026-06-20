using Hhs.VideoGeneratorService.Providers.Video;

namespace Hhs.VideoGeneratorService.Providers;

public interface IVideoProviderResolver
{
    IVideoProvider Resolve(string providerKey);
}

public sealed class VideoProviderResolver(IEnumerable<IVideoProvider> providers) : IVideoProviderResolver
{
    public IVideoProvider Resolve(string providerKey) =>
        providers.FirstOrDefault(x => x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Video provider not found: {providerKey}");
}