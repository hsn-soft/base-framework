using System.Collections.Concurrent;
using Hhs.MockApi.CdnBunnySelf;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<CdnBunnySelfService>();

var app = builder.Build();

var mediaDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media", "cdn-bunny-self");
Directory.CreateDirectory(mediaDir);

// Upload file to CDN (with zone/path logic)
app.MapPost("/upload", async (IFormFile file, CdnBunnySelfService service, CancellationToken ct) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File is required");

    var fileId = await service.UploadAsync(file, mediaDir, ct);
    var relativeUrl = service.GetRelativeUrl(fileId);
    var cdnUrl = $"{service.GetSelfBaseUrl()}/{relativeUrl}";

    return Results.Ok(new
    {
        success = true,
        fileId,
        storageUrl = cdnUrl,
        cdnUrl,
        provider = "bunny-self"
    });
});

// Download file from CDN (with zone/path logic)
app.MapGet("/media/{**path}", async (string path, CdnBunnySelfService service, CancellationToken ct) =>
{
    var filePath = service.GetFilePathFromRelativePath(path, mediaDir);
    if (!File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await File.ReadAllBytesAsync(filePath, ct);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", provider = "bunny-self" }));

app.Run();

namespace Hhs.MockApi.CdnBunnySelf
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = "http://localhost:5071";
    }

    public sealed class CdnBunnySelfService
    {
        private static readonly ConcurrentDictionary<string, CdnFileEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;
        private readonly ILogger<CdnBunnySelfService> _logger;
        private const string ZonePath = "media";
        private const string PathPrefix = "bunny";

        public CdnBunnySelfService(IOptions<MockApiOptions> options, ILogger<CdnBunnySelfService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string mediaDir, CancellationToken ct)
        {
            var fileId = Guid.CreateVersion7().ToString("N");
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uploadDir = Path.Combine(mediaDir, PathPrefix, dateFolder);
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, $"{fileId}_{file.FileName}");

            await using (var stream = file.OpenReadStream())
            {
                await using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream, ct);
            }

            Store[fileId] = new CdnFileEntry
            {
                OriginalFileName = file.FileName,
                FilePath = filePath,
                RelativePath = $"{ZonePath}/{PathPrefix}/{dateFolder}/{Path.GetFileName(filePath)}",
                UploadedAtUtc = DateTime.UtcNow
            };

            _logger.LogInformation("File uploaded to BunnySelf: {FileId}, OriginalName: {FileName}", fileId, file.FileName);
            return fileId;
        }

        public string GetRelativeUrl(string fileId)
        {
            if (Store.TryGetValue(fileId, out var entry))
                return entry.RelativePath;
            return fileId;
        }

        public string GetFilePathFromRelativePath(string relativePath, string mediaDir)
        {
            // Remove zone path from relative path
            var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length > 0 && pathParts[0] == ZonePath)
            {
                // Remove ZonePath from the beginning
                var remainingPath = string.Join(Path.DirectorySeparatorChar, pathParts.Skip(1));
                return Path.Combine(mediaDir, remainingPath);
            }
            return Path.Combine(mediaDir, relativePath);
        }

        public string GetSelfBaseUrl() => _options.Value.SelfBaseUrl;
    }

    public sealed class CdnFileEntry
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public DateTime UploadedAtUtc { get; set; }
    }
}