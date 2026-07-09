using Hhs.Shared.Helper.Providers;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

public interface IAudioProvider
{
    string ProviderKey { get; }
    AudioProviderCapabilities Capabilities { get; }

    Task<AudioCreateResponse> CreateAsync(
        AudioCreateRequest request);

    Task<AudioStatusResponse> GetStatusAsync(
        string providerTrackId);
}

public sealed class AudioCreateRequest
{
    [NotNull] public string InputText { get; set; } = default!;
    [CanBeNull] public string AudioReferenceKey { get; set; }
}

public sealed class AudioCreateResponse
{
    public bool IsCompleted { get; set; }
    [CanBeNull] public string ProviderTrackId { get; set; }
    [CanBeNull] public string ProviderFileUrl { get; set; }
    [CanBeNull] public string FileName { get; set; }
}

public sealed class AudioStatusResponse
{
    public bool IsProcessed { get; set; }
    public bool IsFailed { get; set; }
    [CanBeNull] public string ProviderFileUrl { get; set; }
    [CanBeNull] public string ErrorMessage { get; set; }
    [CanBeNull] public string FileName { get; set; }
}