namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio.ElevenLabs;
public class ElevenLabsSettings
{
    public string ApiKey { get; set; }
    public string ApiBaseUrl { get; set; }
    public string Model { get; set; }
    public float Stability { get; set; }
    public float SimilarityBoost { get; set; }
    public float Speed { get; set; }

    //We'll have default values for Below attributes. Client settings will override them
    public string VoiceId { get; set; }
    public string LanguageCode { get; set; }

}