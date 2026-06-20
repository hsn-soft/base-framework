namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public sealed class DummyFileDownloader : IFileDownloader
{
    public async Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken)
    {
        return await DownloadAsync(fileUrl, extension, null, cancellationToken);
    }

    public async Task<string> DownloadAsync(string fileUrl, string extension, string? providerFileName, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media", "downloads");
        Directory.CreateDirectory(folder);

        string filename;
        if (!string.IsNullOrWhiteSpace(providerFileName))
        {
            // Preserve provider filename: mock_audio_quick_xxx.mp3.txt -> local_mock_audio_quick_xxx.mp3.txt
            filename = $"local_{providerFileName}";
        }
        else
        {
            var fileType = extension switch
            {
                "mp3" => "audio",
                "mp4" => "video",
                _ => "file"
            };
            filename = $"local_{fileType}_{Guid.NewGuid():N}.{extension}";
        }

        var path = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(path, $"downloaded from {fileUrl}", cancellationToken);

        return path;
    }
}

