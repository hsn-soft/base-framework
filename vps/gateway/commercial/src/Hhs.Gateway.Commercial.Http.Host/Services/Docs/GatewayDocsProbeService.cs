using System.Diagnostics;
using Hhs.Gateway.Commercial.Options.Docs;
using Hhs.Gateway.Commercial.Services.Swagger;

namespace Hhs.Gateway.Commercial.Services.Docs;

public interface IGatewayDocsProbeService
{
    Task<EndpointProbeResult?> ProbeEndpointAsync(
        string? url,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GatewayServiceLiveInfo>> ProbeAsync(
        IReadOnlyCollection<SwaggerServiceDefinition> services,
        CancellationToken cancellationToken = default);
}

public sealed class GatewayDocsProbeService(
    IHttpClientFactory httpClientFactory,
    IServiceUrlResolver urlResolver,
    ILogger<GatewayDocsProbeService> logger)
    : IGatewayDocsProbeService
{
    public async Task<IReadOnlyCollection<GatewayServiceLiveInfo>> ProbeAsync(
        IReadOnlyCollection<SwaggerServiceDefinition> services,
        CancellationToken cancellationToken = default)
    {
        var tasks = services.Select(async service =>
        {
            var info = new GatewayServiceLiveInfo
            {
                Definition = service
            };

            try
            {
                string serviceHealthUrl = urlResolver.BuildServiceUrl(service.Key, service.ServiceHealthPath);
                string gatewayHealthUrl = urlResolver.BuildGatewayUrl(service.GatewayHealthPath);

                string serviceRootUrl = urlResolver.BuildServiceUrl(service.Key, "/");
                string gatewayRootUrl = urlResolver.BuildGatewayUrl($"/{service.Key}");

                info.ServiceHealth = await ProbeEndpointAsync(serviceHealthUrl, cancellationToken);
                info.GatewayHealth = await ProbeEndpointAsync(gatewayHealthUrl, cancellationToken);

                info.ServiceRoot = await ProbeEndpointAsync(serviceRootUrl, cancellationToken);
                info.GatewayRoot = await ProbeEndpointAsync(gatewayRootUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Probe failed for service {ServiceKey}", service.Key);
            }

            return info;
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<EndpointProbeResult?> ProbeEndpointAsync(
        string? url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            using var client = httpClientFactory.CreateClient(nameof(GatewayDocsProbeService));

            client.Timeout = TimeSpan.FromSeconds(5);

            var sw = Stopwatch.StartNew();

            using var response = await client.GetAsync(url, cancellationToken);

            sw.Stop();

            string snippet = string.Empty;

            try
            {
                snippet = await response.Content.ReadAsStringAsync(cancellationToken);

                if (snippet.Length > 300)
                    snippet = snippet[..300];
            }
            catch
            {
                snippet = string.Empty;
            }

            return new EndpointProbeResult
            {
                Url = url,
                StatusCode = response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ResponseSnippet = snippet
            };
        }
        catch (Exception ex)
        {
            return new EndpointProbeResult
            {
                Url = url,
                StatusCode = null,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}