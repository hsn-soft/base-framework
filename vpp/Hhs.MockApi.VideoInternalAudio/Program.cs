var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoInternalAudioService>();

var app = builder.Build();

app.MapPost("/video/create", (VideoInternalAudioRequest request, VideoInternalAudioService service) =>
{
    var trackingId = service.CreateRequest(request.OutlineData);
    return Results.Ok(new { provider = "video-internal-audio", trackingId, pollingWindowSec = 120 });
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoInternalAudioService service, CancellationToken ct) =>
{
    var (isReady, videoUrl, message) = await service.GetStatusAsync(trackingId, ct);
    return Results.Ok(new
    {
        provider = "video-internal-audio",
        trackingId,
        isReady,
        remoteVideoUrl = videoUrl,
        message
    });
});

app.Run();

public sealed class VideoInternalAudioTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string OutlineData { get; set; } = default!;
    public string? Result { get; set; }
}

public sealed class VideoInternalAudioService
{
    private readonly Dictionary<string, VideoInternalAudioTrackingEntry> _store = new();
    private readonly object _lock = new object();

    public string CreateRequest(string outlineData)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _store[trackingId] = new VideoInternalAudioTrackingEntry
            {
                TrackingId = trackingId,
                CreatedAtUtc = DateTime.UtcNow,
                OutlineData = outlineData
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
                return (false, null, $"Generating video with internal audio... {elapsed:F0}s of 120s");

            if (entry.Result is null)
            {
                entry.Result = $"https://video-internal-audio-api.internal/video/{trackingId}/output.mp4";
            }

            return (true, entry.Result, "Ready");
        }
    }
}

public sealed record VideoInternalAudioRequest(string OutlineData);
