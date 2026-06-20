using Hhs.TextNormalizerService.Providers.Outline;

namespace Hhs.TextNormalizerService.Providers;

public interface IOutlineProviderResolver
{
    IOutlineProvider Resolve(string providerKey);
}

public sealed class OutlineProviderResolver(IEnumerable<IOutlineProvider> providers) : IOutlineProviderResolver
{
    public IOutlineProvider Resolve(string providerKey) =>
        providers.FirstOrDefault(x => x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Outline provider not found: {providerKey}");
}