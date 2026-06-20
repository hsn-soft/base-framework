using System.Collections.Concurrent;
using Hhs.MockApi.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StorageService>();
builder.Services.AddHttpClient();

var app = builder.Build();

var mediaDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media", "storage");
Directory.CreateDirectory(mediaDir);

// Upload file to central storage
app.MapPost("/upload", async (IFormFile file, StorageService service, CancellationToken ct) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File is required");

    var fileId = await service.UploadAsync(file, mediaDir, ct);

    return Results.Ok(new
    {
        success = true,
        fileId,
        fileName = file.FileName,
        fileSize = file.Length,
        provider = "storage"
    });
});

// Download file from central storage
app.MapGet("/download/{fileId}", async (string fileId, StorageService service, CancellationToken ct) =>
{
    var filePath = service.GetFilePath(fileId, mediaDir);
    if (!File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await File.ReadAllBytesAsync(filePath, ct);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

// List all stored files
app.MapGet("/list", (StorageService service) =>
{
    var files = service.GetAllFiles(mediaDir);
    return Results.Ok(new
    {
        total = files.Count,
        files
    });
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", provider = "storage" }));

app.Run();

namespace Hhs.MockApi.Storage
{
    public sealed class StorageService
    {
        private static readonly ConcurrentDictionary<string, StorageFileEntry> FileStore = new();
        private readonly ILogger<StorageService> _logger;

        public StorageService(ILogger<StorageService> logger)
        {
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string storageDir, CancellationToken ct)
        {
            var fileId = Guid.NewGuid().ToString("N");
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uploadDir = Path.Combine(storageDir, dateFolder);
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, $"{fileId}_{file.FileName}");

            await using (var stream = file.OpenReadStream())
            {
                await using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream, ct);
            }

            FileStore[fileId] = new StorageFileEntry
            {
                OriginalFileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                UploadedAtUtc = DateTime.UtcNow
            };

            _logger.LogInformation("File stored: {FileId}, OriginalName: {FileName}, Size: {Size}",
                fileId, file.FileName, file.Length);

            return fileId;
        }

        public string GetFilePath(string fileId, string storageDir)
        {
            if (FileStore.TryGetValue(fileId, out var entry))
                return entry.FilePath;

            // Fallback: try to find in directory
            var files = Directory.GetFiles(storageDir, $"{fileId}_*", SearchOption.AllDirectories);
            if (files.Length > 0)
                return files[0];

            return Path.Combine(storageDir, fileId);
        }

        public List<object> GetAllFiles(string storageDir)
        {
            return FileStore.Select(kvp => new
            {
                fileId = kvp.Key,
                fileName = kvp.Value.OriginalFileName,
                size = kvp.Value.FileSize,
                uploadedAt = kvp.Value.UploadedAtUtc
            }).Cast<object>().ToList();
        }
    }

    public sealed class StorageFileEntry
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAtUtc { get; set; }
    }
}