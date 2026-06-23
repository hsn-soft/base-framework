using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;

public sealed class FileStorageUploadResponseDto
{
    public bool HasError { get; set; }

    [CanBeNull]
    public string ErrorMessage { get; set; }

    [CanBeNull]
    public string FileTraceId { get; set; }

    [CanBeNull]
    public string FileStorageUrl { get; set; }
}