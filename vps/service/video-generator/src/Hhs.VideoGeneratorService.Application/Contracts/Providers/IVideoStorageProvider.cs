using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers;

public interface IVideoStorageProvider<in TVideoStorageProviderSettings>
    where TVideoStorageProviderSettings : class, new()
{
    Task<FileStorageUploadResponseDto> UploadAsync(FileStorageUploadRequestDto input, TVideoStorageProviderSettings videoStorageProviderSettings, string clientZoneName);
}

public interface IBunnyCdnSelfVideoStorageProvider : IVideoStorageProvider<BunnyCdnSelfStorageSettings>;

public interface IBunnyCdnS3VideoStorageProvider : IVideoStorageProvider<BunnyCdnS3StorageSettings>;