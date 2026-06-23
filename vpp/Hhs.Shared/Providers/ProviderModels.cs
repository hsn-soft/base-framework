namespace Hhs.Shared.Providers;

public enum ProviderExecutionMode
{
    ImmediateResult,
    AsyncPolling
}

public enum VideoAudioInputMode
{
    ProviderCreatesAudio,
    AudioUrlListRequired
}

public sealed class OutlineProviderCapabilities
{
    public string ProviderKey { get; set; } = default!;
    public ProviderExecutionMode ExecutionMode { get; set; }
}

public sealed class AudioProviderCapabilities
{
    public string ProviderKey { get; set; } = default!;
    public ProviderExecutionMode ExecutionMode { get; set; }
}

public sealed class VideoProviderCapabilities
{
    public string ProviderKey { get; set; } = default!;
    public ProviderExecutionMode ExecutionMode { get; set; }
    public VideoAudioInputMode AudioInputMode { get; set; }
}