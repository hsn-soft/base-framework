namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;

public sealed class FileStorageUploadRequestDto
{
    public string LocalFilePath { get; set; }
}
/*
public sealed class VideoStreamStorageUploadRequestDto
{
    [NotNull]
    public string ExternalVideoUrl { get; set; }
}
*/