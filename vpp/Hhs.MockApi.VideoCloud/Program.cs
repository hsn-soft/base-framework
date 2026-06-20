using System.Collections.Concurrent;
using Hhs.MockApi.VideoCloud;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<VideoCloudService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", (VideoRequest request, VideoCloudService service) =>
{
    var trackingId = service.CreateRequest(request.AudioUrls);
    return Results.Ok(new { provider = "video-cloud", trackingId, pollingWindowSec = 150 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoCloudService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error, fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, ct);
    if (!isReady)
        return Results.Ok(new { provider = "video-cloud", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "video-cloud", trackingId, status = "completed", remoteFileUrl = fileUrl, fileName });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    var filePath = VideoCloudService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

namespace Hhs.MockApi.VideoCloud
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
    }

    public sealed class VideoCloudService
    {
        private static readonly ConcurrentDictionary<string, VideoCloudEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;

        public VideoCloudService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string CreateRequest(List<string> audioUrls)
        {
            var trackingId = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow;
            Store[trackingId] = new VideoCloudEntry { AudioUrls = audioUrls, CreatedAt = createdAt };
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
                var audioInfo = entry.AudioUrls?.Count > 0 ? $"Audio URLs: {string.Join(", ", entry.AudioUrls)}" : "No audio";
                await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File\nTracking ID: {trackingId}\n{audioInfo}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/video/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir)
        {
            return Path.Combine(mockFilesDir, $"mock_video_cloud_{trackingId}.mp4.txt");
        }
    }

    public sealed class VideoCloudEntry
    {
        public List<string> AudioUrls { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public sealed record VideoRequest(List<string> AudioUrls);
}