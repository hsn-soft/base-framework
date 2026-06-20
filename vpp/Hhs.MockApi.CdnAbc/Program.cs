using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<CdnAbcService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Upload file to Storage backend (via delegation)
app.MapPost("/upload", async (IFormFile file, CdnAbcService service, CancellationToken ct) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File is required");

    var fileId = await service.UploadAsync(file, ct);
    var cdnUrl = $"{service.GetSelfBaseUrl()}/download/{fileId}";

    return Results.Ok(new
    {
        success = true,
        fileId,
        storageUrl = $"{service.GetStorageBackendUrl()}/download/{fileId}",
        cdnUrl,
        provider = "abc"
    });
});

// Download file from Storage backend
app.MapGet("/download/{fileId}", async (string fileId, CdnAbcService service, CancellationToken ct) =>
{
    var fileStream = await service.DownloadAsync(fileId, ct);
    if (fileStream == null)
        return Results.NotFound();

    return Results.File(fileStream, "application/octet-stream", fileId);
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", provider = "abc" }));

app.Run();

public sealed class MockApiOptions
{
    public string SelfBaseUrl { get; set; } = "http://localhost:5073";
    public string StorageBackendUrl { get; set; } = "http://localhost:5074";
}

public sealed class CdnAbcService
{
    private static readonly ConcurrentDictionary<string, AbcFileEntry> Store = new();
    private readonly IOptions<MockApiOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnAbcService> _logger;

    public CdnAbcService(
        IOptions<MockApiOptions> options,
        HttpClient httpClient,
        ILogger<CdnAbcService> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(IFormFile file, CancellationToken ct)
    {
        var fileId = Guid.NewGuid().ToString("N");

        try
        {
            // Read file into bytes
            await using var memStream = new MemoryStream();
            await file.CopyToAsync(memStream, ct);
            var fileBytes = memStream.ToArray();

            // Delegate to Storage backend
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileBytes), "file", file.FileName);

            var uploadUrl = $"{_options.Value.StorageBackendUrl}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(ct);
                Store[fileId] = new AbcFileEntry
                {
                    OriginalFileName = file.FileName,
                    StorageFileId = fileId,
                    UploadedAtUtc = DateTime.UtcNow
                };

                _logger.LogInformation("File uploaded via ABC to Storage: {FileId}, FileName: {FileName}",
                    fileId, file.FileName);
                return fileId;
            }
            else
            {
                _logger.LogError("Storage backend upload failed: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Storage upload failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file via ABC");
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string fileId, CancellationToken ct)
    {
        try
        {
            if (!Store.TryGetValue(fileId, out _))
                return null;

            // Delegate to Storage backend
            var downloadUrl = $"{_options.Value.StorageBackendUrl}/download/{fileId}";
            var response = await _httpClient.GetAsync(downloadUrl, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync(ct);
            }

            _logger.LogWarning("Storage backend download failed: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file via ABC");
            return null;
        }
    }

    public string GetSelfBaseUrl() => _options.Value.SelfBaseUrl;
    public string GetStorageBackendUrl() => _options.Value.StorageBackendUrl;
}

public sealed class AbcFileEntry
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageFileId { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}
