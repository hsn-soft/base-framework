using Hhs.VideoGeneratorService.Configuration;

namespace Hhs.VideoGeneratorService.Providers;

public interface IStorageService
{
    Task<string> UploadAsync(string localFilePath, CancellationToken cancellationToken);
}

public sealed class DummyStorageService : IStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _storageBaseUrl;

    public DummyStorageService(HttpClient httpClient, StorageProviderSettings storageSettings)
    {
        _httpClient = httpClient;
        _storageBaseUrl = storageSettings.BaseUrl;
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
                    $"{_storageBaseUrl}/storage/upload-binary?fileName={localFileName}",
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
