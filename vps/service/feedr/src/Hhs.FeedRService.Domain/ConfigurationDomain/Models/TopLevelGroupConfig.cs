using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ConfigurationDomain.Models;

/// <summary>
/// Value object representing a top-level ad unit group within a <see cref="NetworkConfiguration"/>.
/// Client-to-AdUnitId mappings are stored in <see cref="CustomerConfiguration"/> instead.
/// </summary>
public sealed class TopLevelGroupConfig
{
    [NotNull]
    public string AdUnitTopLevelCode { get; set; } = string.Empty;
    [NotNull]
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public TopLevelGroupConfig() { }

    public TopLevelGroupConfig(string adUnitTopLevelCode, string displayName, bool isActive)
    {
        AdUnitTopLevelCode = adUnitTopLevelCode ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        IsActive = isActive;
    }
}
