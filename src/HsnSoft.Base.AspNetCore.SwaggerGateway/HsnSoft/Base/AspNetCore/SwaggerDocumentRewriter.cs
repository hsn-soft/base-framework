using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Fetches the document of a service from its internal address and shows it as the gateway's: its address and its paths.</summary>
internal sealed partial class SwaggerDocumentRewriter(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<SwaggerGatewayOptions> options,
    ILogger<SwaggerDocumentRewriter> logger)
{
    internal const string HttpClientName = "HsnSoft.Base.AspNetCore.SwaggerGateway";

    private static readonly JsonSerializerOptions s_write = new() { WriteIndented = true };

    private readonly SwaggerGatewayOptions _options = options.Value;

    public async Task<string> GetAsync(SwaggerServiceDefinition service, string gatewayOrigin, CancellationToken cancellationToken)
    {
        string cacheKey = $"swagger-gateway::{service.Key}::{gatewayOrigin}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        string json = Rewrite(await FetchAsync(service, cancellationToken).ConfigureAwait(false), service, gatewayOrigin);
        cache.Set(cacheKey, json, TimeSpan.FromSeconds(_options.CacheDurationSeconds));
        return json;
    }

    private async Task<string> FetchAsync(SwaggerServiceDefinition service, CancellationToken cancellationToken)
    {
        Uri address = new(new Uri(service.Origin.TrimEnd('/') + "/"), service.SwaggerPath.TrimStart('/'));
        int attempts = _options.RetryCount + 1;
        Exception? last = null;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.HttpTimeoutSeconds));
            try
            {
                using HttpClient client = httpClientFactory.CreateClient(HttpClientName);
                using HttpResponseMessage response = await client.GetAsync(address, timeout.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                }

                last = new HttpRequestException($"The service answered {(int)response.StatusCode}.", null, response.StatusCode);
                if (!IsTemporary(response.StatusCode))
                {
                    break;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                last = exception;
            }

            LogFetchFailed(logger, last, service.Key, attempt, attempts);
            if (attempt < attempts && _options.RetryDelayMilliseconds > 0)
            {
                await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new SwaggerServiceUnavailableException(service.Key, last);
    }

    private static bool IsTemporary(HttpStatusCode status) => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;

    internal static string Rewrite(string json, SwaggerServiceDefinition service, string gatewayOrigin)
    {
        JsonObject root = JsonNode.Parse(json)?.AsObject() ?? throw new JsonException("The document is empty.");
        root["servers"] = new JsonArray { new JsonObject { ["url"] = gatewayOrigin } };

        if (root["paths"] is JsonObject paths)
        {
            JsonObject rewritten = [];
            foreach (KeyValuePair<string, JsonNode?> path in paths)
            {
                rewritten[ShownPath(path.Key, service)] = path.Value?.DeepClone();
            }

            root["paths"] = rewritten;
        }

        return root.ToJsonString(s_write);
    }

    private static string ShownPath(string path, SwaggerServiceDefinition service)
        => path.StartsWith(service.DownstreamPathPrefix, StringComparison.OrdinalIgnoreCase)
            ? service.GatewayPathPrefix + path[service.DownstreamPathPrefix.Length..]
            : path;

    [LoggerMessage(EventId = 5301, EventName = "SWAGGER_GATEWAY_FETCH_FAILED", Level = LogLevel.Warning, Message = "The Swagger document of {Service} could not be fetched (try {Attempt} of {Attempts})")]
    private static partial void LogFetchFailed(ILogger logger, Exception? exception, string service, int attempt, int attempts);
}

internal sealed class SwaggerServiceUnavailableException(string service, Exception? inner)
    : Exception($"The Swagger document of '{service}' could not be fetched.", inner);
