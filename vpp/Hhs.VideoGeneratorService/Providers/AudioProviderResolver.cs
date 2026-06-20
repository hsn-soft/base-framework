using Hhs.VideoGeneratorService.Providers.Audio;

namespace Hhs.VideoGeneratorService.Providers;

public interface IAudioProviderResolver
{
    IAudioProvider Resolve(string providerKey);
}

public sealed class AudioProviderResolver(IEnumerable<IAudioProvider> providers) : IAudioProviderResolver
{
    public IAudioProvider Resolve(string providerKey) =>
        providers.FirstOrDefault(x => x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Audio provider not found: {providerKey}");
}