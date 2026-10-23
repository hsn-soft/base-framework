using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Writes two logs for every request: <c>HttpRequest</c> when it arrives and <c>HttpResponse</c> when it is answered (with the status
/// and how long it took), tied together by a <c>ScopeId</c>; all the requests and messages of one chain share the <c>CorrelationId</c>.
/// The request log is written first on purpose: if the pod dies in the middle (out of memory), it says what was in flight.
/// <c>From</c> is <c>httpclient</c>, <c>To</c> is the endpoint: <c>controller-action</c> in lower case. What the request publishes
/// comes from that endpoint.
/// </summary>
public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IOptions<RequestLoggingOptions> options,
    IServiceProvider services)
{
    private const int MaxCorrelationIdLength = 128;

    // the text is for people reading a log, not for a web page: no \u002B for a plus
    private static readonly JsonSerializerOptions s_headerJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly RequestLoggingOptions _options = options.Value;
    private readonly ILogMasker? _masker = (ILogMasker?)services.GetService(typeof(ILogMasker));

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = CorrelationId(context.Request);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        string scopeId = Guid.CreateVersion7().ToString();
        string to = StepName(context);
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object> { [LogFields.CorrelationId] = correlationId, [LogFields.ScopeId] = scopeId });
        using IDisposable flow = LogFlow.Begin(correlationId, scopeId, to);

        if (!IsLogged(context.Request))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        await InvokeLoggedAsync(context, to).ConfigureAwait(false);
    }

    private async Task InvokeLoggedAsync(HttpContext context, string to)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        string from = LogFlow.HttpClientName;
        Stopwatch watch = Stopwatch.StartNew();

        string? requestBody = _options.LogBodies && _masker is not null ? await ReadRequestBodyAsync(request).ConfigureAwait(false) : null;
        string method = request.Method;
        string? path = request.Path.Value;
        Dictionary<string, object?> requestFields = new()
        {
            [LogFields.LogType] = LogTypes.HttpRequest,
            ["Query"] = request.QueryString.Value,
            ["ContentLength"] = request.ContentLength ?? 0,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString(),
            ["RequestHeaders"] = Headers(request.Headers, isResponse: false),
            ["RequestBody"] = requestBody,
        };
        using (logger.BeginScope(requestFields))
        {
            LogRequest(logger, from, to, method, path);
        }

        Stream originalBody = response.Body;
        MemoryStream? capture = _options.LogBodies && _masker is not null ? new MemoryStream() : null;
        if (capture is not null)
        {
            response.Body = capture;
        }

        Exception? failure = null;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failure = exception;
            response.StatusCode = StatusCodes.Status500InternalServerError;
        }
        finally
        {
            string? responseBody = capture is null ? null : await ReleaseResponseBodyAsync(response, capture, originalBody).ConfigureAwait(false);
            watch.Stop();
            WriteResponseLog(context, from, to, watch.ElapsedMilliseconds, responseBody, failure);
        }

        if (failure is not null)
        {
            // the exception is still the application's to handle: it was logged, not swallowed
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private void WriteResponseLog(HttpContext context, string from, string to, long elapsedMs, string? responseBody, Exception? failure)
    {
        HttpResponse response = context.Response;
        int status = response.StatusCode;
        string headers = Headers(response.Headers, isResponse: true);
        string? userId = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        LogLevel level = failure is not null || status >= StatusCodes.Status500InternalServerError ? LogLevel.Error
            : status >= StatusCodes.Status400BadRequest ? LogLevel.Warning
            : LogLevel.Information;
        Dictionary<string, object?> fields = new()
        {
            [LogFields.LogType] = LogTypes.HttpResponse,
            ["UserId"] = userId,
            ["ContentLength"] = response.ContentLength ?? 0,
            ["ResponseHeaders"] = headers,
            ["ResponseBody"] = responseBody,
        };

        using (logger.BeginScope(fields))
        {
            LogResponse(logger, level, failure, from, to, status, elapsedMs);
        }
    }

    private string CorrelationId(HttpRequest request)
    {
        string? incoming = request.Headers[_options.CorrelationIdHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(incoming) ? Guid.CreateVersion7().ToString() : LogFields.Text(incoming.Trim(), MaxCorrelationIdLength)!;
    }

    private bool IsLogged(HttpRequest request)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        PathString path = request.Path;
        if (!_options.LogHealthChecks && path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !_options.ExcludedPaths.Any(excluded => path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary><c>controller-action</c> for a controller, the endpoint's name for a named one, else <c>method-route</c>; all lower case.</summary>
    internal static string StepName(HttpContext context)
    {
        Endpoint? endpoint = context.GetEndpoint();
        ControllerActionDescriptor? action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is not null)
        {
            return LogFields.StepName(action.ControllerName, action.ActionName);
        }

        if (endpoint?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName is { Length: > 0 } name)
        {
            return name.ToLowerInvariant();
        }

        string route = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? string.Empty;
        return LogFields.StepName(context.Request.Method, route.Trim('/').Replace('/', '-'));
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!IsJson(request.ContentType) || request.ContentLength is null or 0 || request.ContentLength > _options.MaxBodyReadBytes)
        {
            return null;
        }

        request.EnableBuffering();
        using StreamReader reader = new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string text = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;
        return MaskBody(text);
    }

    // hands the captured bytes to the real response stream and returns them as the (masked) text to log
    private async Task<string?> ReleaseResponseBodyAsync(HttpResponse response, MemoryStream capture, Stream originalBody)
    {
        try
        {
            capture.Position = 0;
            string? body = IsJson(response.ContentType) && capture.Length <= _options.MaxBodyReadBytes
                ? MaskBody(Encoding.UTF8.GetString(capture.GetBuffer(), 0, (int)capture.Length))
                : null;

            capture.Position = 0;
            await capture.CopyToAsync(originalBody).ConfigureAwait(false);
            return body;
        }
        finally
        {
            response.Body = originalBody;
            await capture.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string? MaskBody(string text)
        => string.IsNullOrWhiteSpace(text) ? null : LogFields.Text(_masker!.MaskText(text), _options.MaxBodyLength);

    private static bool IsJson(string? contentType)
        => contentType is not null
           && (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase));

    // one JSON text, not one field per header: Graylog creates a field for every name it sees
    private string Headers(IHeaderDictionary headers, bool isResponse)
    {
        Dictionary<string, string> shown = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in headers)
        {
            shown[header.Key] = HeaderValue(header.Key, header.Value.ToString(), isResponse);
        }

        return LogFields.Text(JsonSerializer.Serialize(shown, s_headerJson), _options.MaxBodyLength)!;
    }

    private string HeaderValue(string name, string value, bool isResponse)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!isResponse && name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            return AuthorizationSummary(value);
        }

        if (!isResponse && name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
        {
            return CookieNames(value);
        }

        if (isResponse && name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
        {
            return "Set-Cookie: " + LogFields.Text(value.Split(';', 2)[0].Split('=', 2)[0].Trim(), 30);
        }

        return LogFields.Text(value, _options.MaxHeaderValueLength)!;
    }

    // a credential is never written, only that there was one and how long it was
    private static string AuthorizationSummary(string value)
        => value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? $"Bearer (length: {value["Bearer ".Length..].Trim().Length})"
            : $"AuthHeader (length: {value.Length})";

    private static string CookieNames(string value)
    {
        string[] names = [.. value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(cookie => cookie.Split('=', 2)[0].Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(name => LogFields.Text(name, 30)!)];

        return names.Length == 0 ? "Cookies: <unknown>" : "Cookies: " + string.Join(", ", names);
    }

    [LoggerMessage(EventId = 3000, EventName = "HTTP_REQUEST", Level = LogLevel.Information, Message = "{From} -> {To}: {Method} {Path}")]
    private static partial void LogRequest(ILogger logger, string from, string to, string method, string? path);

    // the level is the outcome: 5xx and exceptions are errors, 4xx are warnings
    [LoggerMessage(EventId = 3001, EventName = "HTTP_RESPONSE", Message = "{From} -> {To}: {Status} in {ElapsedMs} ms")]
    private static partial void LogResponse(ILogger logger, LogLevel level, Exception? exception, string from, string to, int status, long elapsedMs);
}
