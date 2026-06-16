using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioHQService>();

var app = builder.Build();

app.MapPost("/audio/generate", (AudioRequest request, AudioHQService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "audio-hq", trackingId, pollingWindowSec = 60 });
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioHQService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error) = await service.GetStatusAsync(trackingId, ct);
    if (!isReady)
        return Results.Ok(new { provider = "audio-hq", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "audio-hq", trackingId, status = "completed", remoteFileUrl = fileUrl });
});

app.Run();

public sealed class AudioHQService
{
    private static readonly ConcurrentDictionary<string, AudioHQEntry> Store = new();

    public string CreateRequest(string inputText)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        Store[trackingId] = new AudioHQEntry { InputText = inputText, CreatedAt = createdAt };
        return trackingId;
    }

    public async Task<(bool IsReady, string? FileUrl, string? Error)> GetStatusAsync(string trackingId, CancellationToken cancellationToken)
    {
        if (!Store.TryGetValue(trackingId, out var entry))
            return (false, null, "Not found");

        var elapsed = DateTime.UtcNow - entry.CreatedAt;
        if (elapsed.TotalSeconds < 20)
            return (false, null, null);

        var audioId = trackingId;
        return (true, $"https://audio-hq-api.internal/audio/{audioId}/output-hq.mp3", null);
    }
}

public sealed class AudioHQEntry
{
    public string InputText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed record AudioRequest(string InputText);
