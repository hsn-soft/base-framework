namespace Hhs.VideoGeneratorService.Providers;

public interface IAudioProviderResolver
{
    IAudioProvider Resolve(string providerKey);
}

public sealed class AudioProviderResolver : IAudioProviderResolver
{
    private readonly IEnumerable<IAudioProvider> _providers;

    public AudioProviderResolver(IEnumerable<IAudioProvider> providers)
    {
        _providers = providers;
    }

    public IAudioProvider Resolve(string providerKey)
    {
        return _providers.FirstOrDefault(x =>
                   x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Audio provider not found: {providerKey}");
    }
}

public interface IVideoProviderResolver
{
    IVideoProvider Resolve(string providerKey);
}

public sealed class VideoProviderResolver : IVideoProviderResolver
{
    private readonly IEnumerable<IVideoProvider> _providers;

    public VideoProviderResolver(IEnumerable<IVideoProvider> providers)
    {
        _providers = providers;
    }

    public IVideoProvider Resolve(string providerKey)
    {
        return _providers.FirstOrDefault(x =>
                   x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Video provider not found: {providerKey}");
    }
}