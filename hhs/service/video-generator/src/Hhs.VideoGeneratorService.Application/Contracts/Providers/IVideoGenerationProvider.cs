using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers;

public interface IVideoGenerationProvider<in TVideoGenerationProviderSettings, in TCdnStorageSettings>
    where TVideoGenerationProviderSettings : class, new()
    where TCdnStorageSettings : class, new()
{
    Task<VideoGenerationSendResponseDto> SendVideoRequestAsync(VideoGenerationSendRequestDto input, bool isProviderSupportPreSignedStorage, TVideoGenerationProviderSettings videoGenerationProviderSettings, TCdnStorageSettings cdnProviderSettings,string clientZoneName);

    Task<VideoGenerationQueryResponseDto> QueryVideoRequestAsync(VideoGenerationQueryRequestDto input, bool isProviderSupportPreSignedStorage, TVideoGenerationProviderSettings videoGenerationProviderSettings, TCdnStorageSettings cdnProviderSettings, string clientZoneName);

    Task<VideoGenerationDownloadResponseDto> DownloadAsync(VideoGenerationDownloadRequestDto input);
}

public interface IColossyanAiVideoGenerationProvider : IVideoGenerationProvider<ClientColossyanAiSettings, BunnyCdnSelfStorageSettings>;

public interface IYepicAiVideoGenerationProvider : IVideoGenerationProvider<ClientYepicAiSettings, BunnyCdnSelfStorageSettings>;

public interface IHeyGenVideoGenerationProvider : IVideoGenerationProvider<ClientHeyGenSettings, BunnyCdnS3StorageSettings>;

public interface IDidAiVideoGenerationProvider : IVideoGenerationProvider<ClientDidAiSettings, BunnyCdnS3StorageSettings>;

public interface ICreatomateVideoGenerationProvider : IVideoGenerationProvider<ClientCreatomateSettings, BunnyCdnS3StorageSettings>;

