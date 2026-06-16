using Hhs.Shared.Providers;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineProviderXyzTrackingEntry
{
    public string TrackingId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string InputText { get; set; } = default!;
    public string? Result { get; set; }
}

public sealed class OutlineProviderXyz : IOutlineProvider
{
    private static readonly Dictionary<string, OutlineProviderXyzTrackingEntry> _trackingStore = new();
    private static readonly object _lock = new object();

    public string ProviderKey => "outline-xyz";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var trackingId = Guid.NewGuid().ToString();
        var entry = new OutlineProviderXyzTrackingEntry
        {
            TrackingId = trackingId,
            CreatedAtUtc = DateTime.UtcNow,
            InputText = request.InputText
        };

        lock (_lock)
        {
            _trackingStore[trackingId] = entry;
        }

        return Task.FromResult(new OutlineCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = trackingId
        });
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_trackingStore.TryGetValue(providerTrackId, out var entry))
            {
                return Task.FromResult(new OutlineStatusResponse
                {
                    IsCompleted = false,
                    IsFailed = true,
                    ErrorMessage = "Tracking ID not found."
                });
            }

            var elapsedSeconds = (DateTime.UtcNow - entry.CreatedAtUtc).TotalSeconds;

            if (elapsedSeconds < 30)
            {
                return Task.FromResult(new OutlineStatusResponse
                {
                    IsCompleted = false,
                    IsFailed = false,
                    ErrorMessage = "Not ready yet. Wait 30 seconds."
                });
            }

            if (entry.Result is null)
            {
                entry.Result = GenerateDummyScript(entry.InputText);
            }

            return Task.FromResult(new OutlineStatusResponse
            {
                IsCompleted = true,
                IsFailed = false,
                Script = entry.Result
            });
        }
    }

    private static string GenerateDummyScript(string inputText)
    {
        var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"Outline {i + 1}: {line.Trim()}"));
        return outline;
    }

    public static void ClearStore()
    {
        lock (_lock)
        {
            _trackingStore.Clear();
        }
    }

    public static Dictionary<string, OutlineProviderXyzTrackingEntry> GetStore()
    {
        lock (_lock)
        {
            return new Dictionary<string, OutlineProviderXyzTrackingEntry>(_trackingStore);
        }
    }
}
