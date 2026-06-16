using Hhs.Shared.Providers;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineCreateRequest
{
    public string InputText { get; set; } = default!;
}

public sealed class OutlineCreateResponse
{
    public bool IsCompleted { get; set; }
    public string? Script { get; set; }
    public string? ProviderTrackId { get; set; }
}

public sealed class OutlineStatusResponse
{
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? Script { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IOutlineProvider
{
    string ProviderKey { get; }
    OutlineProviderCapabilities Capabilities { get; }

    Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken);

    Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken);
}