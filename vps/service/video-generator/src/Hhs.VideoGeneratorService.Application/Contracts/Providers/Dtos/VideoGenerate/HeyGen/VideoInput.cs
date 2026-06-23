using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class VideoInput
{
    [JsonProperty("character")]
    public CharacterSettings CharacterSettings { get; set;}

    [JsonProperty("voice")]
    public VoiceSettings VoiceSettings { get; set;}

    [JsonProperty("background")]
    public BackgroundSettings BackgroundSettings { get; set;}
}