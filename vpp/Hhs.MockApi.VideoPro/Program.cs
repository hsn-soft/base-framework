using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoProService>();

var app = builder.Build();

app.MapPost("/video/generate", (VideoRequest request, VideoProService service) =>
{
    var trackingId = service.CreateRequest(request.AudioFilePaths);
    return Results.Ok(new { provider = "video-pro", trackingId, pollingWindowSec = 120 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoProService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error) = await service.GetStatusAsync(trackingId, ct);
    if (!isReady)
        return Results.Ok(new { provider = "video-pro", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "video-pro", trackingId, status = "completed", remoteFileUrl = fileUrl });
});

app.Run();

public sealed class VideoProService
{
    private static readonly ConcurrentDictionary<string, VideoProEntry> Store = new();

    public string CreateRequest(List<string> audioFilePaths)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        Store[trackingId] = new VideoProEntry { AudioFilePaths = audioFilePaths, CreatedAt = createdAt };
        return trackingId;
    }

    public async Task<(bool IsReady, string? FileUrl, string? Error)> GetStatusAsync(string trackingId, CancellationToken cancellationToken)
    {
        if (!Store.TryGetValue(trackingId, out var entry))
            return (false, null, "Not found");

        var elapsed = DateTime.UtcNow - entry.CreatedAt;
        if (elapsed.TotalSeconds < 20)
            return (false, null, null);

        var audioInfo = entry.AudioFilePaths?.Count > 0 ? $" (with {entry.AudioFilePaths.Count} audio file(s))" : "";
        return (true, $"https://video-pro-api.internal/video/{trackingId}/output-pro.mp4{audioInfo}", null);
    }
}

public sealed class VideoProEntry
{
    public List<string> AudioFilePaths { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public sealed record VideoRequest(List<string> AudioFilePaths);
