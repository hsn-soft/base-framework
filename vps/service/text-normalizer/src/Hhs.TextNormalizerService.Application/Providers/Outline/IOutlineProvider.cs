using Hhs.Shared.Helper.Providers;
using JetBrains.Annotations;

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
    [CanBeNull] public string ProviderTrackId { get; set; }
}

public sealed class OutlineStatusRequest
{
    public string ProviderTrackId { get; set; } = string.Empty;
}

public class OutlineStatusResponse
{
    public bool IsProcessed { get; set; }
    public bool IsProcessFailed  { get; set; }
    [CanBeNull] public string ErrorMessage { get; set; }

    [CanBeNull] public string OutlinedData { get; set; }
    [CanBeNull] public List<string> Categories { get; set; }
    [CanBeNull] public List<string> Tags { get; set; }
}

public interface IOutlineProvider
{
    string ProviderKey { get; }
    OutlineProviderCapabilities Capabilities { get; }

    Task<OutlineCreateResponse> OutlineOperationAsync(OutlineCreateRequest request);

    Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request);
}