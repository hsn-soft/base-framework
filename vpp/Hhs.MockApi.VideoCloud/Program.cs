using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var mockFilesDir = Path.Combine(Path.GetTempPath(), "mock-provider-files");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", (VideoRequest request) =>
{
    var trackingId = VideoCloudService.CreateRequest(request.AudioUrls);
    return Results.Ok(new { provider = "video-cloud", trackingId, pollingWindowSec = 120 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, CancellationToken ct) =>
{
    var (isReady, fileUrl, error) = await VideoCloudService.GetStatusAsync(trackingId, mockFilesDir, ct);
    if (!isReady)
        return Results.Ok(new { provider = "video-cloud", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "video-cloud", trackingId, status = "completed", remoteFileUrl = fileUrl });
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

public sealed class VideoCloudService
{
    private static readonly ConcurrentDictionary<string, VideoCloudEntry> Store = new();

    public static string CreateRequest(List<string> audioUrls)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        Store[trackingId] = new VideoCloudEntry { AudioUrls = audioUrls, CreatedAt = createdAt };
        return trackingId;
    }

    public static async Task<(bool IsReady, string? FileUrl, string? Error)> GetStatusAsync(string trackingId, string mockFilesDir, CancellationToken cancellationToken)
    {
        if (!Store.TryGetValue(trackingId, out var entry))
            return (false, null, "Not found");

        var elapsed = DateTime.UtcNow - entry.CreatedAt;
        if (elapsed.TotalSeconds < 20)
            return (false, null, null);

        var filePath = GetFilePath(trackingId, mockFilesDir);
        if (!System.IO.File.Exists(filePath))
        {
            var audioInfo = entry.AudioUrls?.Count > 0 ? $"Audio URLs: {string.Join(", ", entry.AudioUrls)}" : "No audio";
            await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File\nTracking ID: {trackingId}\n{audioInfo}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
        }

        var downloadUrl = $"http://localhost:5046/video/download/{trackingId}";
        return (true, downloadUrl, null);
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
