using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<CdnBunnyS3Service>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Upload file to S3 backend
app.MapPost("/upload", async (IFormFile file, CdnBunnyS3Service service, CancellationToken ct) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File is required");

    var fileId = await service.UploadAsync(file, ct);
    var cdnUrl = $"{service.GetSelfBaseUrl()}/download/{fileId}";

    return Results.Ok(new
    {
        success = true,
        fileId,
        storageUrl = $"s3://{service.GetBucket()}/{fileId}",
        cdnUrl,
        provider = "bunny-s3"
    });
});

// Download file from S3
app.MapGet("/download/{fileId}", async (string fileId, CdnBunnyS3Service service, CancellationToken ct) =>
{
    var fileStream = await service.DownloadAsync(fileId, ct);
    if (fileStream == null)
        return Results.NotFound();

    return Results.File(fileStream, "application/octet-stream", fileId);
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", provider = "bunny-s3" }));

app.Run();

public sealed class MockApiOptions
{
    public string SelfBaseUrl { get; set; } = "http://localhost:5072";
    public string S3Endpoint { get; set; } = "http://localhost:9000";
    public string S3AccessKey { get; set; } = "minioadmin";
    public string S3SecretKey { get; set; } = "minioadmin";
    public string S3Bucket { get; set; } = "videos";
    public string S3Region { get; set; } = "us-east-1";
}

public sealed class CdnBunnyS3Service
{
    private static readonly ConcurrentDictionary<string, S3FileEntry> Store = new();
    private readonly IOptions<MockApiOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnBunnyS3Service> _logger;

    public CdnBunnyS3Service(
        IOptions<MockApiOptions> options,
        HttpClient httpClient,
        ILogger<CdnBunnyS3Service> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(IFormFile file, CancellationToken ct)
    {
        var fileId = Guid.NewGuid().ToString("N");
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var s3Key = $"{dateFolder}/{fileId}_{file.FileName}";

        try
        {
            // Read file into bytes
            await using var memStream = new MemoryStream();
            await file.CopyToAsync(memStream, ct);
            var fileBytes = memStream.ToArray();

            // MOCK IMPLEMENTATION: Save to local disk instead of real S3
            // In production, this would use AWS SDK or MinIO client
            var mockStorageDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "uploads", "s3");
            Directory.CreateDirectory(mockStorageDir);

            var uploadDir = Path.Combine(mockStorageDir, dateFolder);
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, $"{fileId}_{file.FileName}");
            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes, ct);

            Store[fileId] = new S3FileEntry
            {
                S3Key = s3Key,
                OriginalFileName = file.FileName,
                UploadedAtUtc = DateTime.UtcNow
            };

            _logger.LogInformation("File uploaded to S3 (MOCK): {FileId}, Key: {S3Key}, LocalPath: {LocalPath}",
                fileId, s3Key, filePath);
            return fileId;

            /* REAL S3 IMPLEMENTATION (commented out for mock):
            // Construct S3-compatible upload URL
            var uploadUrl = $"{_options.Value.S3Endpoint}/{_options.Value.S3Bucket}/{s3Key}";

            // Upload to S3 (or MinIO)
            using var content = new ByteArrayContent(fileBytes);
            var response = await _httpClient.PutAsync(uploadUrl, content, ct);

            if (response.IsSuccessStatusCode)
            {
                Store[fileId] = new S3FileEntry
                {
                    S3Key = s3Key,
                    OriginalFileName = file.FileName,
                    UploadedAtUtc = DateTime.UtcNow
                };

                _logger.LogInformation("File uploaded to S3: {FileId}, Key: {S3Key}", fileId, s3Key);
                return fileId;
            }
            else
            {
                _logger.LogError("S3 upload failed: {StatusCode}, Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync(ct));
                throw new InvalidOperationException($"S3 upload failed: {response.StatusCode}");
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3");
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string fileId, CancellationToken ct)
    {
        try
        {
            if (!Store.TryGetValue(fileId, out var entry))
                return null;

            // MOCK IMPLEMENTATION: Read from local disk
            var mockStorageDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "uploads", "s3");
            var filePath = Path.Combine(mockStorageDir, entry.S3Key);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("S3 file not found (MOCK): {FilePath}", filePath);
                return null;
            }

            var fileContent = await System.IO.File.ReadAllBytesAsync(filePath, ct);
            _logger.LogInformation("File downloaded from S3 (MOCK): {FileId}, LocalPath: {FilePath}",
                fileId, filePath);
            return new MemoryStream(fileContent);

            /* REAL S3 IMPLEMENTATION (commented out for mock):
            var downloadUrl = $"{_options.Value.S3Endpoint}/{_options.Value.S3Bucket}/{entry.S3Key}";
            var response = await _httpClient.GetAsync(downloadUrl, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync(ct);
            }

            _logger.LogWarning("S3 download failed: {StatusCode}", response.StatusCode);
            return null;
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from S3");
            return null;
        }
    }

    public string GetSelfBaseUrl() => _options.Value.SelfBaseUrl;
    public string GetBucket() => _options.Value.S3Bucket;
}

public sealed class S3FileEntry
{
    public string S3Key { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}
