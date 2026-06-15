namespace Hhs.VideoGeneratorService.Providers;

public sealed class AudioProviderCreateResponse
{
    public string ProviderRequestId { get; set; } = default!;
    public string ProviderFileUrl { get; set; } = default!;
}

public interface IAudioProvider
{
    Task<AudioProviderCreateResponse> CreateAudioAsync(
        string inputText,
        CancellationToken cancellationToken);
}

public sealed class DummyAudioProvider : IAudioProvider
{
    public Task<AudioProviderCreateResponse> CreateAudioAsync(
        string inputText,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new AudioProviderCreateResponse
        {
            ProviderRequestId = Guid.NewGuid().ToString("N"),
            ProviderFileUrl = $"https://dummy-provider/audio/{Guid.NewGuid():N}.mp3"
        });
    }
}

public sealed class VideoProviderCreateResponse
{
    public string ProviderRequestId { get; set; } = default!;
    public string ProviderFileUrl { get; set; } = default!;
}

public interface IVideoProvider
{
    Task<VideoProviderCreateResponse> CreateVideoAsync(
        string videoInputJson,
        List<string> audioUrls,
        CancellationToken cancellationToken);
}

public sealed class DummyVideoProvider : IVideoProvider
{
    public Task<VideoProviderCreateResponse> CreateVideoAsync(
        string videoInputJson,
        List<string> audioUrls,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new VideoProviderCreateResponse
        {
            ProviderRequestId = Guid.NewGuid().ToString("N"),
            ProviderFileUrl = $"https://dummy-provider/video/{Guid.NewGuid():N}.mp4"
        });
    }
}

public interface IFileDownloader
{
    Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken);
}

public sealed class DummyFileDownloader : IFileDownloader
{
    public async Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(Path.GetTempPath(), "hhs-saga-demo");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, $"{Guid.NewGuid():N}.{extension}");
        await File.WriteAllTextAsync(path, $"dummy downloaded from {fileUrl}", cancellationToken);

        return path;
    }
}

public interface IStorageService
{
    Task<string> UploadAsync(string localFilePath, CancellationToken cancellationToken);
}

public sealed class DummyStorageService : IStorageService
{
    public Task<string> UploadAsync(string localFilePath, CancellationToken cancellationToken)
    {
        return Task.FromResult($"https://dummy-storage/{Path.GetFileName(localFilePath)}");
    }
}