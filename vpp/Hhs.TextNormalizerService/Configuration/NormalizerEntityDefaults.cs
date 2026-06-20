namespace Hhs.TextNormalizerService.Configuration;

public sealed class NormalizerEntityDefaults
{
    public const string SectionName = "EntityDefaults:Normalizer";

    public int MaxOutlinePollingCount { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 5;
}
