namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class ColossyanAiSettings: VideoGenerationProviderSettings
{
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
}