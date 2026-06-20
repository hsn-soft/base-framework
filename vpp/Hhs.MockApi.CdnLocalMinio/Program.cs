using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<MinioService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Upload file to MinIO via CDN API
app.MapPost("/upload", async (IFormFile file, MinioService service, CancellationToken ct) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "File is required" });

    var result = await service.UploadAsync(file, ct);
    return Results.Ok(result);
});

// Download file from MinIO via CDN API
app.MapGet("/download/{fileId}", async (string fileId, MinioService service, CancellationToken ct) =>
{
    var fileStream = await service.DownloadAsync(fileId, ct);
    if (fileStream == null)
        return Results.NotFound(new { error = "File not found" });

    return Results.File(fileStream, "application/octet-stream");
});

// List uploaded files (for debugging)
app.MapGet("/list", (MinioService service) =>
{
    var files = service.GetAllFiles();
    return Results.Ok(new { total = files.Count, files });
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", provider = "local-minio", backend = "MinIO" }));

app.Run();

public sealed class MockApiOptions
{
    public string SelfBaseUrl { get; set; } = "http://localhost:5070";
    public string MinioEndpoint { get; set; } = "http://localhost:9000";
    public string MinioAccessKey { get; set; } = "minioadmin";
    public string MinioSecretKey { get; set; } = "minioadmin";
    public string MinioBucket { get; set; } = "videos";
    public string CdnZonePath { get; set; } = "media";
    public string CdnPathPrefix { get; set; } = "minio";
}

public sealed class MinioService
{
    private static readonly ConcurrentDictionary<string, MinioFileEntry> FileStore = new();
    private readonly IOptions<MockApiOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MinioService> _logger;

    public MinioService(IOptions<MockApiOptions> options, HttpClient httpClient, ILogger<MinioService> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<object> UploadAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString("N")[..8];
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var storedFileName = $"{fileId}_{file.FileName}";
            var s3Key = $"{dateFolder}/{storedFileName}";

            // Read file bytes
            await using var memStream = new MemoryStream();
            await file.CopyToAsync(memStream, ct);
            var fileBytes = memStream.ToArray();

            // Upload to MinIO
            var minioUrl = $"{_options.Value.MinioEndpoint}/{_options.Value.MinioBucket}/{s3Key}";

            _logger.LogInformation(
                "Uploading to MinIO: {MinioUrl}, FileId: {FileId}, StoredName: {StoredFileName}, Size: {Size}",
                minioUrl, fileId, storedFileName, fileBytes.Length);

            using var content = new ByteArrayContent(fileBytes);
            var response = await _httpClient.PutAsync(minioUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MinIO upload failed: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"MinIO upload failed: {response.StatusCode}");
            }

            // Build URLs
            var storageUrl = minioUrl; // Internal: direct MinIO URL
            var cdnUrl = $"{_options.Value.SelfBaseUrl}/{_options.Value.CdnZonePath}/{_options.Value.CdnPathPrefix}/{s3Key}";

            // Store metadata
            FileStore[fileId] = new MinioFileEntry
            {
                FileId = fileId,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                S3Key = s3Key,
                FileSize = fileBytes.Length,
                StorageUrl = storageUrl,
                CdnUrl = cdnUrl,
                UploadedAtUtc = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Upload successful: FileId={FileId}, StoredName={StoredFileName}, MinioPath={MinioPath}, CdnUrl={CdnUrl}",
                fileId, storedFileName, s3Key, cdnUrl);

            return new
            {
                success = true,
                fileId,
                fileName = file.FileName,
                storedFileName,
                fileSize = fileBytes.Length,
                s3Key,
                storageUrl,
                cdnUrl,
                minioInfo = new
                {
                    endpoint = _options.Value.MinioEndpoint,
                    bucket = _options.Value.MinioBucket,
                    path = s3Key
                },
                provider = "local-minio",
                backend = "MinIO",
                instructions = new
                {
                    viewInMinioUI = $"{_options.Value.MinioEndpoint} → bucket '{_options.Value.MinioBucket}' → path '{s3Key}'",
                    downloadViaApi = $"GET /download/{fileId}",
                    verifyFile = "Download and compare SHA256 hash with original"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string fileId, CancellationToken ct)
    {
        try
        {
            if (!FileStore.TryGetValue(fileId, out var entry))
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return null;
            }

            var minioUrl = entry.StorageUrl;

            _logger.LogInformation(
                "Downloading from MinIO: {FileId}, StoredName: {StoredFileName}, MinioUrl: {MinioUrl}",
                fileId, entry.StoredFileName, minioUrl);

            var response = await _httpClient.GetAsync(minioUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MinIO download failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var fileStream = await response.Content.ReadAsStreamAsync(ct);

            _logger.LogInformation(
                "Download successful: {FileId}, Size: {Size}",
                fileId, fileStream.Length);

            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed: {FileId}", fileId);
            return null;
        }
    }

    public List<object> GetAllFiles()
    {
        return FileStore.Select(kvp => new
        {
            fileId = kvp.Key,
            originalFileName = kvp.Value.OriginalFileName,
            storedFileName = kvp.Value.StoredFileName,
            fileSize = kvp.Value.FileSize,
            s3Key = kvp.Value.S3Key,
            minioPath = $"{_options.Value.MinioBucket}/{kvp.Value.S3Key}",
            cdnUrl = kvp.Value.CdnUrl,
            uploadedAt = kvp.Value.UploadedAtUtc
        }).Cast<object>().ToList();
    }
}

public sealed class MinioFileEntry
{
    public string FileId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public string CdnUrl { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}
