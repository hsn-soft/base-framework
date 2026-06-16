using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.MockApis;

namespace Hhs.TextNormalizerService.Providers;

public sealed class CustomXyzOutlineProvider : IOutlineProvider
{
    private readonly MockCustomXyzOutlineApi _mockApi;

    public string ProviderKey => "custom-xyz";

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling
    };

    public CustomXyzOutlineProvider()
    {
        _mockApi = new MockCustomXyzOutlineApi();
    }

    public Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var trackingId = _mockApi.CreateOutlineRequest(request.InputText);

        return Task.FromResult(new OutlineCreateResponse
        {
            IsCompleted = false,
            ProviderTrackId = trackingId
        });
    }

    public async Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        var (isReady, outline, errorMessage) = await _mockApi.GetOutlineStatusAsync(providerTrackId, cancellationToken);

        if (!isReady)
        {
            return new OutlineStatusResponse
            {
                IsCompleted = false,
                IsFailed = false,
                ErrorMessage = errorMessage
            };
        }

        return new OutlineStatusResponse
        {
            IsCompleted = true,
            IsFailed = false,
            Script = outline
        };
    }

    public static void ClearMockStore()
    {
        MockCustomXyzOutlineApi.ClearStore();
    }

    public static Dictionary<string, MockCustomXyzTrackingEntry> GetMockStore()
    {
        return MockCustomXyzOutlineApi.GetStore();
    }
}