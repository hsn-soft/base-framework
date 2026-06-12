namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Yepic;

public class YepicAiSettings: VideoGenerationProviderSettings
{
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
    public string AvatarId { get; set; }
    public string VoiceId { get; set; }
    public string VideoTitle { get; set; }
    public string Visibility { get; set; }
}