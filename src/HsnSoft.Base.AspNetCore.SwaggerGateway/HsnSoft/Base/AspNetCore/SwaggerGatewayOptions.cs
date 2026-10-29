using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>The services whose Swagger documents the gateway shows (section <c>SwaggerGateway</c>).</summary>
public sealed class SwaggerGatewayOptions
{
    public const string SectionName = "SwaggerGateway";

    /// <summary>
    /// The address clients reach the gateway at; the "Try it out" of every document sends its requests there. When it is not given it is the address of the
    /// request that asked for the document (behind a proxy, set it, or use the forwarded headers).
    /// </summary>
    public string? PublicOrigin { get; set; }

    public List<SwaggerServiceDefinition> Services { get; set; } = [];

    /// <summary>How long a rewritten document is kept, so the UI does not call the services every time it is opened.</summary>
    public int CacheDurationSeconds { get; set; } = 60;

    /// <summary>How long the gateway waits for the document of a service.</summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>The extra tries after the first one when a service answers with a temporary failure (408, 429, 5xx) or times out.</summary>
    public int RetryCount { get; set; } = 2;

    public int RetryDelayMilliseconds { get; set; } = 300;
}

/// <summary>A service behind the gateway.</summary>
public sealed class SwaggerServiceDefinition
{
    /// <summary>The name in the address of its document (<c>/swagger/services/{Key}/swagger.json</c>): letters, digits and dashes.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The name in the list of the UI; the key when it is not given.</summary>
    public string? DisplayName { get; set; }

    /// <summary>The internal address of the service, where the gateway fetches its document from (<c>http://orders:8080</c>).</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>Where the service serves its document.</summary>
    public string SwaggerPath { get; set; } = "/swagger/v1/swagger.json";

    /// <summary>The start of the paths the service has (<c>/api/orders</c> or empty) that the gateway has another start for: see <see cref="GatewayPathPrefix"/>.</summary>
    public string DownstreamPathPrefix { get; set; } = string.Empty;

    /// <summary>What replaces <see cref="DownstreamPathPrefix"/> in the paths shown, so they are the paths of the gateway (<c>/orders/v1</c>).</summary>
    public string GatewayPathPrefix { get; set; } = string.Empty;

    internal string Name => string.IsNullOrWhiteSpace(DisplayName) ? Key : DisplayName;
}

internal sealed partial class SwaggerGatewayOptionsValidator : IValidateOptions<SwaggerGatewayOptions>
{
    [GeneratedRegex("^[a-zA-Z0-9-]+$")]
    private static partial Regex KeyFormat();

    public ValidateOptionsResult Validate(string? name, SwaggerGatewayOptions options)
    {
        List<string> failures = [];
        if (options.PublicOrigin is { Length: > 0 } && !IsOrigin(options.PublicOrigin))
        {
            failures.Add("SwaggerGateway:PublicOrigin must be an address (http://host or https://host).");
        }

        foreach (IGrouping<string, SwaggerServiceDefinition> duplicate in options.Services.GroupBy(service => service.Key, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            failures.Add($"SwaggerGateway:Services has the key '{duplicate.Key}' more than once.");
        }

        foreach (SwaggerServiceDefinition service in options.Services)
        {
            if (!KeyFormat().IsMatch(service.Key))
            {
                failures.Add($"SwaggerGateway:Services: the key '{service.Key}' must be letters, digits and dashes.");
            }

            if (!IsOrigin(service.Origin))
            {
                failures.Add($"SwaggerGateway:Services:{service.Key}: Origin must be an address (http://host:port).");
            }

            if (!service.SwaggerPath.StartsWith('/'))
            {
                failures.Add($"SwaggerGateway:Services:{service.Key}: SwaggerPath must start with '/'.");
            }
        }

        if (options.CacheDurationSeconds < 0 || options.HttpTimeoutSeconds < 1 || options.RetryCount < 0 || options.RetryDelayMilliseconds < 0)
        {
            failures.Add("SwaggerGateway: CacheDurationSeconds, RetryCount and RetryDelayMilliseconds cannot be negative and HttpTimeoutSeconds must be at least 1.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsOrigin(string? text)
        => Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https";
}
