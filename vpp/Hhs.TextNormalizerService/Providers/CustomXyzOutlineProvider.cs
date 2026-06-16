using Hhs.Shared.Providers;

namespace Hhs.TextNormalizerService.Providers;

public sealed class CustomXyzOutlineProvider : IOutlineProvider
{
    public string ProviderKey => "custom-xyz";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new OutlineCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = Guid.NewGuid().ToString("N")
        });
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new OutlineStatusResponse
        {
            IsCompleted = true,
            Script = $"Custom XYZ completed script for track: {providerTrackId}"
        });
    }
}