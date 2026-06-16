using Hhs.Shared.Providers;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OpenAiOutlineProvider : IOutlineProvider
{
    public string ProviderKey => "openai";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new OutlineCreateResponse
        {
            IsCompleted = true,
            Script = $"OpenAI script: {request.InputText[..Math.Min(80, request.InputText.Length)]}"
        });
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("OpenAI outline provider does not support polling.");
    }
}