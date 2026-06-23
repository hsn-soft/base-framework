using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class VoiceSettings
{
    [JsonProperty("type")]
    public string Type { get; set;}
}

public class TextVoiceSettings : VoiceSettings
{
    [JsonProperty("voice_id")]
    public string VoiceId { get; set; }

    [JsonProperty("input_text")]
    public string InputText { get; set; }

    [JsonProperty("speed")]
    public float Speed { get; set; }

    [JsonProperty("pitch")]
    public int Pitch { get; set; }

    [JsonProperty("emotion")]
    public string Emotion { get; set; }
}

public class AudioVoiceSettings : VoiceSettings
{
    [JsonProperty("audio_url")]
    public string AudioUrl { get; set; }

    [JsonProperty("audio_asset_id")]
    public string AudioAssetId { get; set; }
}