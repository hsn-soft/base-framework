namespace Hhs.VideoGeneratorService.Providers;

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