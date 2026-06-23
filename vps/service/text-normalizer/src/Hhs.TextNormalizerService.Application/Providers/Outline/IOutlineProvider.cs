using Hhs.Shared.Helper.Providers;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineCreateRequest
{
    public string OutlinePrompt { get; set; } = string.Empty;
    public string OutlineInput { get; set; } = string.Empty;

    public string EngineModel { get; set; } = string.Empty;

    public bool UseStructuredOutput { get; set; } = false;
}

public sealed class OutlineCreateResponse : OutlineStatusResponse
{
    // Pooling Track Id
    public string? ProviderTrackId { get; set; }
}

public sealed class OutlineStatusRequest
{
    public string ProviderTrackId { get; set; } = string.Empty;
}

public class OutlineStatusResponse
{
    public bool IsProcessed { get; set; }
    public bool IsProcessFailed  { get; set; }
    public string? ErrorMessage { get; set; }

    public string? OutlinedData { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? Tags { get; set; }
}

public interface IOutlineProvider
{
    string ProviderKey { get; }
    OutlineProviderCapabilities Capabilities { get; }

    Task<OutlineCreateResponse> CreateAsync(OutlineCreateRequest request);

    Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request);
}