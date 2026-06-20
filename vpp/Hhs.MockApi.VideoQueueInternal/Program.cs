using System.Collections.Concurrent;
using Hhs.MockApi.VideoQueueInternal;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<VideoQueueInternalService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", (VideoRequest request, VideoQueueInternalService service) =>
{
    var trackingId = service.CreateRequest();
    return Results.Ok(new { provider = "video-queue-internal", trackingId, pollingWindowSec = 150 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoQueueInternalService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error, fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, ct);
    if (!isReady)
        return Results.Ok(new { provider = "video-queue-internal", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "video-queue-internal", trackingId, status = "completed", remoteFileUrl = fileUrl, fileName });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    var filePath = VideoQueueInternalService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

namespace Hhs.MockApi.VideoQueueInternal
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
    }

    public sealed class VideoQueueInternalService
    {
        private static readonly ConcurrentDictionary<string, VideoQueueInternalEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;

        public VideoQueueInternalService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string CreateRequest()
        {
            var trackingId = Guid.NewGuid().ToString("N");
            Store[trackingId] = new VideoQueueInternalEntry { CreatedAt = DateTime.UtcNow };
            return trackingId;
        }

        public async Task<(bool IsReady, string? FileUrl, string? Error, string? FileName)> GetStatusAsync(string trackingId, string mockFilesDir, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found", null);

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < 30)
                return (false, null, null, null);

            var filePath = GetFilePath(trackingId, mockFilesDir);
            var fileName = Path.GetFileName(filePath);

            if (!System.IO.File.Exists(filePath))
            {
                await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File (Internal Audio)\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/video/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir)
        {
            return Path.Combine(mockFilesDir, $"mock_video_queue_internal_{trackingId}.mp4.txt");
        }
    }

    public sealed class VideoQueueInternalEntry
    {
        public DateTime CreatedAt { get; set; }
    }

    public sealed record VideoRequest(List<string> AudioUrls);
}