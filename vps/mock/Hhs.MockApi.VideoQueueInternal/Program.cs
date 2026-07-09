using System.Collections.Concurrent;
using Hhs.MockApi.VideoQueueInternal;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<VideoQueueInternalService>();
builder.Services.AddHostedService<StaleMockFileCleanupWorker>();

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<MockApiOptions>>().Value;
string mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.MediaDirectory);
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", (VideoRequest request, VideoQueueInternalService service) =>
{
    string trackingId = service.CreateRequest();
    string downloadUrl = $"{service.GetBaseUrl()}/video/download/{trackingId}";
    return Results.Ok(new { provider = "video-queue-internal", trackingId, remoteFileUrl = downloadUrl, pollingWindowSec = options.PollingWindowSeconds });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoQueueInternalService service, CancellationToken ct) =>
{
    (bool isReady, string? fileUrl, string? error, string? fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, options, ct);
    if (!isReady)
    {
        return Results.Ok(error != null
            ? new { provider = "video-queue-internal", trackingId, status = "failed", error }
            : new { provider = "video-queue-internal", trackingId, status = "processing", error = string.Empty });
    }

    return Results.Ok(new
    {
        provider = "video-queue-internal",
        trackingId,
        status = "completed",
        remoteFileUrl = fileUrl,
        fileName
    });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    string filePath = VideoQueueInternalService.GetFilePath(trackingId, mockFilesDir, options);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    byte[] fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

namespace Hhs.MockApi.VideoQueueInternal
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
        public int ProcessingDelaySeconds { get; set; } = 30;
        public int PollingWindowSeconds { get; set; } = 150;
        public string MediaDirectory { get; set; } = "media";
    }

    public sealed class VideoQueueInternalService
    {
        private static readonly ConcurrentDictionary<string, VideoQueueInternalEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;
        private const string ProviderName = "video-queue-internal";

        public VideoQueueInternalService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public string CreateRequest()
        {
            string trackingId = Guid.CreateVersion7().ToString("N").ToLower();
            Store[trackingId] = new VideoQueueInternalEntry { CreatedAt = DateTime.UtcNow };
            return trackingId;
        }

        public async Task<(bool IsReady, string? FileUrl, string? Error, string? FileName)> GetStatusAsync(string trackingId, string mockFilesDir, MockApiOptions options, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found", null);

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < options.ProcessingDelaySeconds)
                return (false, null, null, null);

            string filePath = GetFilePath(trackingId, mockFilesDir, options);
            string fileName = Path.GetFileName(filePath);

            if (!System.IO.File.Exists(filePath))
            {
                await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File (Internal Audio)\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            string downloadUrl = $"{_options.Value.SelfBaseUrl}/video/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir, MockApiOptions options)
        {
            return Path.Combine(mockFilesDir, $"{ProviderName}-{trackingId}.avi");
        }
    }

    public sealed class VideoQueueInternalEntry
    {
        public DateTime CreatedAt { get; set; }
    }

    public sealed record VideoRequest(string InputText);

    /// <summary>
    /// Deletes generated mock media files older than 1 hour (by creation time) so the mock's
    /// media directory doesn't grow unbounded across repeated local/CI runs. MinIO/CDN storage
    /// is a separate mock (Hhs.MockApi.CdnLocalMinio) and is intentionally untouched here.
    /// </summary>
    public sealed class StaleMockFileCleanupWorker(IOptions<MockApiOptions> options, ILogger<StaleMockFileCleanupWorker> logger) : BackgroundService
    {
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(1);
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.Value.MediaDirectory);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupStaleFiles(mockFilesDir);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Stale mock file cleanup failed for directory {MockFilesDir}", mockFilesDir);
                }

                try
                {
                    await Task.Delay(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void CleanupStaleFiles(string mockFilesDir)
        {
            if (!Directory.Exists(mockFilesDir)) return;

            var cutoffUtc = DateTime.UtcNow - MaxFileAge;

            foreach (string filePath in Directory.EnumerateFiles(mockFilesDir))
            {
                try
                {
                    if (File.GetCreationTimeUtc(filePath) >= cutoffUtc) continue;

                    File.Delete(filePath);
                    logger.LogInformation("Deleted stale mock file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete stale mock file: {FilePath}", filePath);
                }
            }
        }
    }
}