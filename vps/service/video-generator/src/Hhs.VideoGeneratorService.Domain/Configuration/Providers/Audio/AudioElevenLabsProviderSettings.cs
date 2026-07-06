using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;

public sealed class AudioElevenLabsProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioElevenLabs";

    public string Model { get; set; } = "eleven_multilingual_v2";
    public string VoiceId { get; set; }
    public string LanguageCode { get; set; }
    public float Stability { get; set; } = 0.5f;
    public float SimilarityBoost { get; set; } = 0.75f;
    public float Speed { get; set; } = 1.0f;
}
