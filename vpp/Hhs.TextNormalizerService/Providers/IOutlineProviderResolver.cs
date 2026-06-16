using Hhs.TextNormalizerService.Providers;

public interface IOutlineProviderResolver
{
    IOutlineProvider Resolve(string providerKey);
}

public sealed class OutlineProviderResolver : IOutlineProviderResolver
{
    private readonly IEnumerable<IOutlineProvider> _providers;

    public OutlineProviderResolver(IEnumerable<IOutlineProvider> providers)
    {
        _providers = providers;
    }

    public IOutlineProvider Resolve(string providerKey)
    {
        return _providers.FirstOrDefault(x =>
                   x.ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Outline provider not found: {providerKey}");
    }
}