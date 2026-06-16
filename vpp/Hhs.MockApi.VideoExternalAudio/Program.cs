var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoExternalAudioService>();

var app = builder.Build();

app.MapPost("/video/create", (VideoExternalAudioRequest request, VideoExternalAudioService service) =>
{
    var trackingId = service.CreateRequest(request.OutlineData, request.AudioUrls);
    return Results.Ok(new { provider = "video-external-audio", trackingId, pollingWindowSec = 120 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoExternalAudioService service, CancellationToken ct) =>
{
    var (isReady, videoUrl, message) = await service.GetStatusAsync(trackingId, ct);
    return Results.Ok(new
    {
        provider = "video-external-audio",
        trackingId,
        isReady,
        remoteVideoUrl = videoUrl,
        message
    });
});

app.Run();

public sealed class VideoExternalAudioTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string OutlineData { get; set; } = default!;
    public List<string> AudioUrls { get; set; } = new();
    public string? Result { get; set; }
}

public sealed class VideoExternalAudioService
{
    private readonly Dictionary<string, VideoExternalAudioTrackingEntry> _store = new();
    private readonly object _lock = new object();

    public string CreateRequest(string outlineData, List<string> audioUrls)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _store[trackingId] = new VideoExternalAudioTrackingEntry
            {
                TrackingId = trackingId,
                CreatedAtUtc = DateTime.UtcNow,
                OutlineData = outlineData,
                AudioUrls = audioUrls
            };
        }
        return trackingId;
    }

    public async Task<(bool IsReady, string? VideoUrl, string Message)> GetStatusAsync(string trackingId, CancellationToken ct)
    {
        await Task.Delay(100, ct);

        lock (_lock)
        {
            if (!_store.TryGetValue(trackingId, out var entry))
                return (false, null, "Tracking ID not found");

            var elapsed = (DateTime.UtcNow - entry.CreatedAtUtc).TotalSeconds;
            if (elapsed < 120)
                return (false, null, $"Generating video with external audio... {elapsed:F0}s of 120s (audio: {entry.AudioUrls.Count} files)");

            if (entry.Result is null)
            {
                entry.Result = $"https://video-external-audio-api.internal/video/{trackingId}/output.mp4";
            }

            return (true, entry.Result, "Ready");
        }
    }
}

public sealed record VideoExternalAudioRequest(string OutlineData, List<string> AudioUrls);
