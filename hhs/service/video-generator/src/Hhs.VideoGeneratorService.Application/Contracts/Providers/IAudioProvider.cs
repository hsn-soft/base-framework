using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio.ElevenLabs;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers;

public interface IAudioProvider<in TAudioProviderSettings>
    where TAudioProviderSettings : class, new()
{
    Task<AudioGenerationSendResponseDto> SendAudioAsync(AudioGenerationSendRequestDto input,TAudioProviderSettings audioProviderSettings);
}

public interface IElevenLabsAudioProvider : IAudioProvider<ElevenLabsSettings>;