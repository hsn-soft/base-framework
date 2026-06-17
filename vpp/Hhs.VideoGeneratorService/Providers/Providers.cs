namespace Hhs.VideoGeneratorService.Providers;

public interface IFileDownloader
{
    Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken);
    Task<string> DownloadAsync(string fileUrl, string extension, string? providerFileName, CancellationToken cancellationToken);
}

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

public interface IStorageService
{
    Task<string> UploadAsync(string localFilePath, CancellationToken cancellationToken);
}

public sealed class DummyStorageService : IStorageService
{
    private readonly HttpClient _httpClient;

    public DummyStorageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> UploadAsync(string localFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
            var localFileName = Path.GetFileName(localFilePath);

            using (var content = new ByteArrayContent(fileBytes))
            {
                var response = await _httpClient.PostAsync(
                    $"http://localhost:5048/storage/upload-binary?fileName={localFileName}",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
                var remoteUrl = jsonDoc.RootElement.GetProperty("url").GetString();

                return remoteUrl ?? throw new InvalidOperationException("No URL in storage response");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload file {localFilePath} to storage service", ex);
        }
    }
}