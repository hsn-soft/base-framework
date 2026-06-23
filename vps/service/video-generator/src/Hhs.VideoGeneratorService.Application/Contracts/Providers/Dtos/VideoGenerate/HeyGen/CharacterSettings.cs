using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class CharacterSettings
{
    [JsonProperty("text")]
    public string Type { get; set;}
}

public class AvatarSettings : CharacterSettings
{
    [JsonProperty("avatar_id")]
    public string AvatarId { get; set; }

    [JsonProperty("scale")]
    public float Scale { get; set; }

    [JsonProperty("avatar_style")]
    public string AvatarStyle { get; set; }

    [JsonProperty("offset")]
    public Offset Offset { get; set; }

    [JsonProperty("matting")]
    public bool Matting { get; set;}

    [JsonProperty("circle_background_color")]
    public string CircleBackgroundColor { get; set; }
}