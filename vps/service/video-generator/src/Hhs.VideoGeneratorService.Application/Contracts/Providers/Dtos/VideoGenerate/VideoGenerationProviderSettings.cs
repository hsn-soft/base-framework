namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public class VideoGenerationProviderSettings
{
    public bool IsProviderAudioOperationEnabled { get; set; }
    public bool IsEnabledWaitAudioFileGeneration{ get; set; }
    public bool IsEnabledAudioFileDownloadOperation { get; set; }

    public bool IsProviderSupportPreSignedStorage { get; set; }
}