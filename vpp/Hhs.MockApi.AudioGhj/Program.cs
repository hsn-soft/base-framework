var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioGhjService>();

var app = builder.Build();

app.MapPost("/audio/create", (AudioRequest request, AudioGhjService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "audio-ghj", trackingId, pollingWindowSec = 60 });
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioGhjService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, message) = await service.GetStatusAsync(trackingId, ct);
    return Results.Ok(new
    {
        provider = "audio-ghj",
        trackingId,
        isReady,
        remoteFileUrl = fileUrl,
        message
    });
});

app.Run();

public sealed class AudioGhjTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string InputText { get; set; } = default!;
    public string? Result { get; set; }
}

public sealed class AudioGhjService
{
    private readonly Dictionary<string, AudioGhjTrackingEntry> _store = new();
    private readonly object _lock = new object();

    public string CreateRequest(string inputText)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _store[trackingId] = new AudioGhjTrackingEntry
            {
                TrackingId = trackingId,
                CreatedAtUtc = DateTime.UtcNow,
                InputText = inputText
            };
        }
        return trackingId;
    }

    public async Task<(bool IsReady, string? FileUrl, string Message)> GetStatusAsync(string trackingId, CancellationToken ct)
    {
        await Task.Delay(100, ct);

        lock (_lock)
        {
            if (!_store.TryGetValue(trackingId, out var entry))
                return (false, null, "Tracking ID not found");

            var elapsed = (DateTime.UtcNow - entry.CreatedAtUtc).TotalSeconds;
            if (elapsed < 60)
                return (false, null, $"Processing... {elapsed:F0}s of 60s");

            if (entry.Result is null)
            {
                entry.Result = $"https://audio-ghj-api.internal/audio/{trackingId}/output.mp3";
            }

            return (true, entry.Result, "Ready");
        }
    }
}

public sealed record AudioRequest(string InputText);
