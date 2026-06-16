using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OutlineDetailedService>();

var app = builder.Build();

app.MapPost("/outline/generate", (OutlineRequest request, OutlineDetailedService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "outline-detailed", trackingId, pollingWindowSec = 30 });
});

app.MapGet("/outline/status/{trackingId}", async (string trackingId, OutlineDetailedService service, CancellationToken ct) =>
{
    var (isReady, script, error) = await service.GetStatusAsync(trackingId, ct);
    if (!isReady)
        return Results.Ok(new { provider = "outline-detailed", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "outline-detailed", trackingId, status = "completed", script });
});

app.Run();

public sealed class OutlineDetailedService
{
    private static readonly ConcurrentDictionary<string, DetailedOutlineEntry> Store = new();

    public string CreateRequest(string inputText)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        Store[trackingId] = new DetailedOutlineEntry { InputText = inputText, CreatedAt = createdAt };
        return trackingId;
    }

    public async Task<(bool IsReady, string? Script, string? Error)> GetStatusAsync(string trackingId, CancellationToken cancellationToken)
    {
        if (!Store.TryGetValue(trackingId, out var entry))
            return (false, null, "Not found");

        var elapsed = DateTime.UtcNow - entry.CreatedAt;
        if (elapsed.TotalSeconds < 20)
            return (false, null, null);

        var lines = entry.InputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"[Detailed] Point {i + 1}: {line.Trim()} (analyzed)"));
        return (true, outline, null);
    }
}

public sealed class DetailedOutlineEntry
{
    public string InputText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed record OutlineRequest(string InputText);
