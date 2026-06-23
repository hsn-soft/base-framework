using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hhs.Gateway.Commercial.Options.Docs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Services.Swagger;

public interface ISwaggerDocumentRewriter
{
    Task<string> GetRewrittenDocumentAsync(
        SwaggerServiceDefinition serviceDefinition,
        string gatewayBaseUrl,
        CancellationToken cancellationToken = default);
}

public sealed class SwaggerDocumentRewriter(
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IOptions<SwaggerAggregationOptions> aggregationOptions,
    IServiceUrlResolver urlResolver,
    ILogger<SwaggerDocumentRewriter> logger
) : ISwaggerDocumentRewriter
{
    private readonly SwaggerAggregationOptions _aggregationOptions = aggregationOptions.Value;

    public async Task<string> GetRewrittenDocumentAsync(
        SwaggerServiceDefinition serviceDefinition,
        string gatewayBaseUrl,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = BuildCacheKey(serviceDefinition.Key, gatewayBaseUrl);

        if (memoryCache.TryGetValue(cacheKey, out string? cachedJson) && !string.IsNullOrWhiteSpace(cachedJson))
        {
            logger.LogDebug("Swagger rewrite cache hit. ServiceKey={ServiceKey}", serviceDefinition.Key);
            return cachedJson;
        }

        logger.LogDebug("Swagger rewrite cache miss. ServiceKey={ServiceKey}", serviceDefinition.Key);

        string downstreamSwaggerJson = await FetchSwaggerWithRetryAsync(serviceDefinition, cancellationToken);

        string rewrittenJson = RewriteOpenApiDocument(
            downstreamSwaggerJson,
            gatewayBaseUrl,
            serviceDefinition.DownstreamPathPrefix,
            serviceDefinition.GatewayPathPrefix,
            serviceDefinition.Key);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_aggregationOptions.CacheDurationSeconds),
            SlidingExpiration = TimeSpan.FromSeconds(Math.Max(10, _aggregationOptions.CacheDurationSeconds / 2))
        };

        memoryCache.Set(cacheKey, rewrittenJson, cacheOptions);

        return rewrittenJson;
    }

    private async Task<string> FetchSwaggerWithRetryAsync(
        SwaggerServiceDefinition serviceDefinition,
        CancellationToken cancellationToken)
    {
        string swaggerUrl = urlResolver.BuildServiceUrl(
            serviceDefinition.Key,
            "/swagger/v1/swagger.json");

        int totalAttempts = _aggregationOptions.RetryCount + 1;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= totalAttempts; attempt++)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_aggregationOptions.HttpTimeoutSeconds));

            try
            {
                using var client = httpClientFactory.CreateClient(nameof(SwaggerDocumentRewriter));

                logger.LogInformation(
                    "Swagger json fetch attempt. ServiceKey={ServiceKey}, Attempt={Attempt}/{TotalAttempts}, Url={Url}",
                    serviceDefinition.Key,
                    attempt,
                    totalAttempts,
                    swaggerUrl);

                using var response = await client.GetAsync(
                    swaggerUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    linkedCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(linkedCts.Token);
                }

                if (!IsTransientStatusCode(response.StatusCode) || attempt == totalAttempts)
                {
                    string responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

                    throw new HttpRequestException(
                        $"Swagger json alınamadı. ServiceKey={serviceDefinition.Key}, StatusCode={(int)response.StatusCode}, Body={responseBody}");
                }

                logger.LogWarning(
                    "Transient swagger fetch status code. ServiceKey={ServiceKey}, Attempt={Attempt}/{TotalAttempts}, StatusCode={StatusCode}",
                    serviceDefinition.Key,
                    attempt,
                    totalAttempts,
                    (int)response.StatusCode);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;

                logger.LogWarning(
                    ex,
                    "Swagger fetch timeout. ServiceKey={ServiceKey}, Attempt={Attempt}/{TotalAttempts}",
                    serviceDefinition.Key,
                    attempt,
                    totalAttempts);
            }
            catch (Exception ex)
            {
                lastException = ex;

                logger.LogWarning(
                    ex,
                    "Swagger fetch failed. ServiceKey={ServiceKey}, Attempt={Attempt}/{TotalAttempts}",
                    serviceDefinition.Key,
                    attempt,
                    totalAttempts);
            }

            if (attempt < totalAttempts && _aggregationOptions.RetryDelayMilliseconds > 0)
            {
                await Task.Delay(_aggregationOptions.RetryDelayMilliseconds, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Swagger json alınamadı. ServiceKey={serviceDefinition.Key}",
            lastException);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;

        return code == 408
               || code == 429
               || code == 500
               || code == 502
               || code == 503
               || code == 504;
    }

    private static string BuildCacheKey(string serviceKey, string gatewayBaseUrl) { return $"swagger-rewrite::{serviceKey}::{gatewayBaseUrl}"; }

    private static string RewriteOpenApiDocument(
        string swaggerJson,
        string gatewayBaseUrl,
        string downstreamPrefix,
        string gatewayPrefix,
        string gatewayServiceKey)
    {
        var root = JsonNode.Parse(swaggerJson)?.AsObject()
                   ?? throw new InvalidOperationException("Swagger JSON parse edilemedi.");

        root["servers"] = new JsonArray { new JsonObject { ["url"] = gatewayBaseUrl } };

        if (root["paths"] is JsonObject pathsObject)
        {
            var rewrittenPaths = new JsonObject();

            foreach (var pathItem in pathsObject)
            {
                string oldPath = pathItem.Key;

                string newPath = string.Empty;
                if (oldPath.Equals("/"))
                {
                    newPath=$"/{gatewayServiceKey}";
                }
                else
                {
                    newPath = oldPath.StartsWith(downstreamPrefix, StringComparison.OrdinalIgnoreCase)
                        ? gatewayPrefix + oldPath[downstreamPrefix.Length..]
                        : oldPath;
                }

                rewrittenPaths[newPath] = pathItem.Value?.DeepClone();
            }

            root["paths"] = rewrittenPaths;
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}