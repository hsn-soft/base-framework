using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.MockApis;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OpenAiOutlineProvider : IOutlineProvider
{
    private readonly MockOpenAiOutlineApi _mockApi;

    public string ProviderKey => "openai";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public OpenAiOutlineProvider()
    {
        _mockApi = new MockOpenAiOutlineApi();
    }

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _mockApi.GenerateOutlineAsync(request.InputText, cancellationToken);

        return new OutlineCreateResponse
        {
            IsCompleted = true,
            Script = script
        };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("OpenAI outline provider does not support polling.");
    }
}