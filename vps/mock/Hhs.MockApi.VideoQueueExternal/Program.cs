using System.Collections.Concurrent;
using Hhs.MockApi.VideoQueueExternal;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<VideoQueueExternalService>();

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<MockApiOptions>>().Value;
string mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.MediaDirectory);
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", (VideoRequest request, VideoQueueExternalService service) =>
{
    string trackingId = service.CreateRequest(request.AudioUrls);
    string downloadUrl = $"{service.GetBaseUrl()}/video/download/{trackingId}";
    return Results.Ok(new { provider = "video-queue-external", trackingId, remoteFileUrl = downloadUrl, pollingWindowSec = options.PollingWindowSeconds });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoQueueExternalService service, CancellationToken ct) =>
{
    (bool isReady, string? fileUrl, string? error, string? fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, options, ct);
    if (!isReady)
    {
        return Results.Ok(error != null
            ? new { provider = "video-queue-external", trackingId, status = "failed", error }
            : new { provider = "video-queue-external", trackingId, status = "processing", error = string.Empty });
    }
    return Results.Ok(new { provider = "video-queue-external", trackingId, status = "completed", remoteFileUrl = fileUrl, fileName });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    string filePath = VideoQueueExternalService.GetFilePath(trackingId, mockFilesDir, options);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    byte[] fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

namespace Hhs.MockApi.VideoQueueExternal
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
        public int ProcessingDelaySeconds { get; set; } = 30;
        public int PollingWindowSeconds { get; set; } = 150;
        public string MediaDirectory { get; set; } = "media";
    }

    public sealed class VideoQueueExternalService
    {
        private static readonly ConcurrentDictionary<string, VideoQueueExternalEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;
        private const string ProviderName = "video-queue-external";

        public VideoQueueExternalService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public string CreateRequest(List<string> audioUrls)
        {
            var trackingId = Guid.CreateVersion7().ToString("N").ToLower();
            var createdAt = DateTime.UtcNow;
            Store[trackingId] = new VideoQueueExternalEntry { AudioUrls = audioUrls, CreatedAt = createdAt };
            return trackingId;
        }

        public async Task<(bool IsReady, string? FileUrl, string? Error, string? FileName)> GetStatusAsync(string trackingId, string mockFilesDir, MockApiOptions options, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found", null);

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < options.ProcessingDelaySeconds)
                return (false, null, null, null);

            var filePath = GetFilePath(trackingId, mockFilesDir, options);
            var fileName = Path.GetFileName(filePath);

            if (!System.IO.File.Exists(filePath))
            {
                var audioInfo = entry.AudioUrls?.Count > 0 ? $"Audio URLs: {string.Join(", ", entry.AudioUrls)}" : "No audio";
                await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File (External Audio)\nTracking ID: {trackingId}\n{audioInfo}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/video/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir, MockApiOptions options)
        {
            return Path.Combine(mockFilesDir, $"{ProviderName}-{trackingId}.avi");
        }
    }

    public sealed class VideoQueueExternalEntry
    {
        public List<string> AudioUrls { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }

    public sealed record VideoRequest(List<string> AudioUrls);
}
