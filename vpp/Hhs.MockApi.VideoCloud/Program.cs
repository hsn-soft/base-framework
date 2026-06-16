using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoCloudService>();

var app = builder.Build();

app.MapPost("/video/generate", (VideoRequest request, VideoCloudService service) =>
{
    var trackingId = service.CreateRequest(request.AudioUrls);
    return Results.Ok(new { provider = "video-cloud", trackingId, pollingWindowSec = 120 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoCloudService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error) = await service.GetStatusAsync(trackingId, ct);
    if (!isReady)
        return Results.Ok(new { provider = "video-cloud", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "video-cloud", trackingId, status = "completed", remoteFileUrl = fileUrl });
});

app.Run();

public sealed class VideoCloudService
{
    private static readonly ConcurrentDictionary<string, VideoCloudEntry> Store = new();

    public string CreateRequest(List<string> audioUrls)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        Store[trackingId] = new VideoCloudEntry { AudioUrls = audioUrls, CreatedAt = createdAt };
        return trackingId;
    }

    public async Task<(bool IsReady, string? FileUrl, string? Error)> GetStatusAsync(string trackingId, CancellationToken cancellationToken)
    {
        if (!Store.TryGetValue(trackingId, out var entry))
            return (false, null, "Not found");

        var elapsed = DateTime.UtcNow - entry.CreatedAt;
        if (elapsed.TotalSeconds < 20)
            return (false, null, null);

        var audioInfo = entry.AudioUrls?.Count > 0 ? $" (with {entry.AudioUrls.Count} audio url(s))" : "";
        return (true, $"https://video-cloud-api.internal/video/{trackingId}/output-cloud.mp4{audioInfo}", null);
    }
}

public sealed class VideoCloudEntry
{
    public List<string> AudioUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public sealed record VideoRequest(List<string> AudioUrls);
