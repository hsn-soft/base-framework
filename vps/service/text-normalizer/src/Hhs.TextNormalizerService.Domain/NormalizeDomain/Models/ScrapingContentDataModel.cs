using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;

public sealed class ScrapingContentDataModel
{
    [NotNull] public string Title { get; set; }

    public DateTime? ReleaseTimeUtc { get; set; }

    [CanBeNull] public string Spot { get; set; }

    [CanBeNull] public string Details { get; set; }

    [CanBeNull] public string ImageUrl { get; set; }
}
