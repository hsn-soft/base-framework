using System.Collections.Concurrent;
using Hhs.MockApi.OutlineQueue;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/outline/generate", (OutlineRequest request) =>
{
    var trackingId = OutlineQueueService.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "outline-queue", trackingId, pollingWindowSec = 30 });
});

app.MapGet("/outline/status/{trackingId}", async (string trackingId, CancellationToken ct) =>
{
    var (isReady, script, error) = await OutlineQueueService.GetStatusAsync(trackingId, ct);
    if (!isReady)
        return Results.Ok(new { provider = "outline-queue", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "outline-queue", trackingId, status = "completed", script });
});

app.Run();

namespace Hhs.MockApi.OutlineQueue
{
    public sealed class OutlineQueueService
    {
        private static readonly ConcurrentDictionary<string, DetailedOutlineEntry> Store = new();

        public static string CreateRequest(string inputText)
        {
            var trackingId = Guid.NewGuid().ToString("N");
            Store[trackingId] = new DetailedOutlineEntry { InputText = inputText, CreatedAt = DateTime.UtcNow };
            return trackingId;
        }

        public static async Task<(bool IsReady, string? Script, string? Error)> GetStatusAsync(string trackingId, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found");

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < 20)
                return (false, null, null);

            var lines = entry.InputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var outline = string.Join("\n", lines.Select((line, i) => $"[Queue] Point {i + 1}: {line.Trim()} (analyzed)"));
            return (true, outline, null);
        }
    }

    public sealed class DetailedOutlineEntry
    {
        public string InputText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed record OutlineRequest(string InputText);
}