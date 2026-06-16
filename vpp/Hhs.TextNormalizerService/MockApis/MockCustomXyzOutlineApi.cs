namespace Hhs.TextNormalizerService.MockApis;

public sealed class MockCustomXyzTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string InputText { get; set; } = default!;
    public string? Result { get; set; }
}

public sealed class MockCustomXyzOutlineApi
{
    private static readonly Dictionary<string, MockCustomXyzTrackingEntry> _store = new();
    private static readonly object _lock = new object();

    public string CreateOutlineRequest(string inputText)
    {
        var trackingId = Guid.NewGuid().ToString("N");
        var entry = new MockCustomXyzTrackingEntry
        {
            TrackingId = trackingId,
            CreatedAtUtc = DateTime.UtcNow,
            InputText = inputText
        };

        lock (_lock)
        {
            _store[trackingId] = entry;
        }

        return trackingId;
    }

    public async Task<(bool IsReady, string? Outline, string? ErrorMessage)> GetOutlineStatusAsync(
        string trackingId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        lock (_lock)
        {
            if (!_store.TryGetValue(trackingId, out var entry))
            {
                return (false, null, "Tracking ID not found");
            }

            var elapsedSeconds = (DateTime.UtcNow - entry.CreatedAtUtc).TotalSeconds;

            if (elapsedSeconds < 30)
            {
                return (false, null, $"Not ready yet. Elapsed: {elapsedSeconds:F0}s, Wait for 30s total");
            }

            if (entry.Result is null)
            {
                var lines = entry.InputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                var outline = string.Join("\n", lines.Select((line, i) => $"[CustomXyz] Outline {i + 1}: {line.Trim()}"));
                entry.Result = outline;
            }

            return (true, entry.Result, null);
        }
    }

    public static void ClearStore()
    {
        lock (_lock)
        {
            _store.Clear();
        }
    }

    public static Dictionary<string, MockCustomXyzTrackingEntry> GetStore()
    {
        lock (_lock)
        {
            return new Dictionary<string, MockCustomXyzTrackingEntry>(_store);
        }
    }
}
