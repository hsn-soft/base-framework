namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public sealed class DummyFileDownloader : IFileDownloader
{
    public async Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken)
    {
        return await DownloadAsync(fileUrl, extension, null, cancellationToken);
    }

    public async Task<string> DownloadAsync(string fileUrl, string extension, string? providerFileName, CancellationToken cancellationToken)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media", "downloads");
        var diLocal = Directory.CreateDirectory(folder);

        string filename;
        if (!string.IsNullOrWhiteSpace(providerFileName))
        {
            // Preserve provider filename: mock_audio_quick_xxx.mp3.txt -> local_mock_audio_quick_xxx.mp3.txt
            filename = $"local_{providerFileName}";
        }
        else
        {
            string fileType = extension switch
            {
                "mp3" => "audio",
                "mp4" => "video",
                _ => "file"
            };
            filename = $"local_{fileType}_{Guid.NewGuid():N}.{extension}";
        }

        string path = Path.Combine(diLocal.FullName, filename);
        await File.WriteAllTextAsync(path, $"downloaded from {fileUrl}", cancellationToken);

        return path;
    }
}