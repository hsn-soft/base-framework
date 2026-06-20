using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Configuration;

public sealed class NormalizerRetrySettings : RetrySettingsBase
{
    public const string SectionName = "Retry:Normalizer";

    public int RetryWorkerIntervalSeconds { get; set; } = 10;
}
