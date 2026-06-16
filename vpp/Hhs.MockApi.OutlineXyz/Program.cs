var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OutlineXyzService>();

var app = builder.Build();

app.MapPost("/outline/create", (OutlineRequest request, OutlineXyzService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "outline-xyz", trackingId, pollingWindowSec = 30 });
});

app.MapGet("/outline/status/{trackingId}", async (string trackingId, OutlineXyzService service, CancellationToken ct) =>
{
    var (isReady, outline, message) = await service.GetStatusAsync(trackingId, ct);
    return Results.Ok(new
    {
        provider = "outline-xyz",
        trackingId,
        isReady,
        script = outline,
        message
    });
});

app.MapGet("/outline/store", (OutlineXyzService service) =>
{
    var store = service.GetStore();
    return Results.Ok(new
    {
        count = store.Count,
        entries = store.Values.Select(e => new
        {
            e.TrackingId,
            e.CreatedAtUtc,
            elapsedSeconds = (DateTime.UtcNow - e.CreatedAtUtc).TotalSeconds,
            isReady = (DateTime.UtcNow - e.CreatedAtUtc).TotalSeconds >= 30,
            e.Result
        }).ToList()
    });
});

app.Run();

public sealed class OutlineXyzTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string InputText { get; set; } = default!;
    public string? Result { get; set; }
}

public sealed class OutlineXyzService
{
    private readonly Dictionary<string, OutlineXyzTrackingEntry> _store = new();
    private readonly object _lock = new object();

    public string CreateRequest(string inputText)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _store[trackingId] = new OutlineXyzTrackingEntry
            {
                TrackingId = trackingId,
                CreatedAtUtc = DateTime.UtcNow,
                InputText = inputText
            };
        }
        return trackingId;
    }

    public async Task<(bool IsReady, string? Outline, string Message)> GetStatusAsync(string trackingId, CancellationToken ct)
    {
        await Task.Delay(100, ct);

        lock (_lock)
        {
            if (!_store.TryGetValue(trackingId, out var entry))
                return (false, null, "Tracking ID not found");

            var elapsed = (DateTime.UtcNow - entry.CreatedAtUtc).TotalSeconds;
            if (elapsed < 30)
                return (false, null, $"Processing... {elapsed:F0}s of 30s");

            if (entry.Result is null)
            {
                var lines = entry.InputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                entry.Result = string.Join("\n", lines.Select((line, i) => $"[XYZ] Outline {i + 1}: {line.Trim()}"));
            }

            return (true, entry.Result, "Ready");
        }
    }

    public Dictionary<string, OutlineXyzTrackingEntry> GetStore()
    {
        lock (_lock)
        {
            return new Dictionary<string, OutlineXyzTrackingEntry>(_store);
        }
    }
}

public sealed record OutlineRequest(string InputText);
