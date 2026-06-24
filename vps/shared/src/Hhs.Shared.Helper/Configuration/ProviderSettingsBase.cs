using JetBrains.Annotations;

namespace Hhs.Shared.Helper.Configuration;

public abstract class ProviderSettingsBase
{
    public string BaseUrl { get; set; } = default!;
    [CanBeNull] public string APIKey { get; set; }
}
