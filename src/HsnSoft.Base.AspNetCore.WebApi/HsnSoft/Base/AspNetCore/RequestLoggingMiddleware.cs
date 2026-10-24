using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Writes two logs for every request: <c>HttpRequest</c> when it arrives and <c>HttpResponse</c> when it is answered (with the status
/// and how long it took), tied together by a <c>ScopeId</c>; all the requests and messages of one chain share the <c>CorrelationId</c>.
/// The request log is written first on purpose: if the pod dies in the middle (out of memory), it says what was in flight.
/// <c>From</c> is <c>httpclient</c>, <c>To</c> is the endpoint: <c>controller-action</c> in lower case, or the request path. What the request publishes
/// comes from that endpoint.
/// </summary>
public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IOptions<RequestLoggingOptions> options,
    IServiceProvider services)
{
    private const int MaxCorrelationIdLength = 128;

    /// <summary>Where <see cref="GlobalExceptionHandler"/> leaves the exception it answered, so the response log can say it.</summary>
    internal const string HandledExceptionKey = "HsnSoft.Base.AspNetCore.HandledException";

    /// <summary>Where the result filter leaves the class of the payload it wrapped, so the response body is masked by what that class says.</summary>
    internal const string ResponseShapeKey = "HsnSoft.Base.AspNetCore.ResponseShape";
    private const string GrpcContentType = "application/grpc";

    // the text is for people reading a log, not for a web page: no \u002B for a plus
    private static readonly JsonSerializerOptions s_headerJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestLoggingOptions _options = options.Value;
    private readonly ILogMasker? _masker = (ILogMasker?)services.GetService(typeof(ILogMasker));
    private readonly IAuthenticationSchemeProvider? _schemes = services.GetService<IAuthenticationSchemeProvider>();
    private readonly IRequestLogEnricher[] _enrichers = [.. services.GetServices<IRequestLogEnricher>()];

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = CorrelationId(context.Request);

        // what this service forwards (a proxy copies the request headers) carries the chain on to the next one
        context.Request.Headers[_options.CorrelationIdHeader] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // a request is told from the others by the RequestId ASP.NET Core puts on every log of it; its request and response logs share it
        string scopeId = context.TraceIdentifier;
        string to = StepName(context);
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object> { [LogFields.CorrelationId] = correlationId });
        using IDisposable flow = LogFlow.Begin(correlationId, scopeId, to);

        if (!IsLogged(context.Request))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (_options.Role == RequestLoggingRole.Gateway)
        {
            await InvokeGatewayAsync(context, to).ConfigureAwait(false);
            return;
        }

        // the gateway in front of this service logs only what a service did not: this one says it logged the request
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.LoggedByHeader] = AppDomain.CurrentDomain.FriendlyName;
            return Task.CompletedTask;
        });
        await InvokeLoggedAsync(context, to).ConfigureAwait(false);
    }

    private async Task InvokeLoggedAsync(HttpContext context, string to)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        string from = LogFlow.HttpClientName;
        Stopwatch watch = Stopwatch.StartNew();

        string? requestBody = _options.LogBodies && _masker is not null ? await ReadRequestBodyAsync(request, BodyType(context)).ConfigureAwait(false) : null;
        string method = request.Method;
        string? shownPath = LogFields.Text(request.Path.Value, _options.MaxUrlLength);
        Dictionary<string, object?> requestFields = RequestFields(context, LogTypes.HttpRequest, requestBody, await AuthenticatedUserAsync(context).ConfigureAwait(false));

        using (logger.BeginScope(requestFields))
        {
            LogRequest(logger, from, to, method, shownPath);
        }

        // measure what goes in and out without holding any of it back: a file is streamed, not buffered
        Stream originalRequestBody = request.Body;
        CountingReadStream requestBytes = new(originalRequestBody);
        request.Body = requestBytes;
        IHttpResponseBodyFeature originalResponseBody = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        ResponseBodyTee responseBytes = new(originalResponseBody, () => CapturesResponseBody(context), _options.MaxBodyReadBytes);
        context.Features.Set<IHttpResponseBodyFeature>(responseBytes);

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
            watch.Stop();
            string? responseBody = responseBytes.Body is { } text ? MaskBody(text, context.Items.TryGetValue(ResponseShapeKey, out object? shape) ? shape as Type : null, _options.MaxResponseBodyLength) : null;
            WriteResponseLog(context, from, to, watch.ElapsedMilliseconds, responseBody, requestBytes.Count, responseBytes.Count, failure);
            request.Body = originalRequestBody;
            context.Features.Set(originalResponseBody);
        }

        if (failure is not null)
        {
            // the exception is still the application's to handle: it was logged, not swallowed
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    // only a small JSON answer that is not compressed and not a file is worth a copy; everything else is only counted
    private bool CapturesResponseBody(HttpContext context)
    {
        HttpResponse response = context.Response;
        return _options.LogBodies && _masker is not null && IsJson(response.ContentType)
               && !response.Headers.ContainsKey(HeaderNames.ContentEncoding)
               && !response.Headers.ContainsKey(HeaderNames.ContentDisposition)
               && !context.Items.ContainsKey(FileResultLogFilter.ItemKey);
    }

    // what is known of a request: the same fields in the log of a service and in the log a gateway writes for what it answered itself
    private Dictionary<string, object?> RequestFields(HttpContext context, string logType, string? requestBody, ClaimsPrincipal? user)
    {
        HttpRequest request = context.Request;
        Dictionary<string, object?> requestFields = new()
        {
            [LogFields.LogType] = logType,
            ["Query"] = LogFields.Text(request.QueryString.Value, _options.MaxUrlLength),
            ["RequestBytes"] = request.ContentLength ?? 0,   // what the request says it is: the body is not read yet
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString(),
            ["UserId"] = UserId(user),
            ["UserRoles"] = Roles(user),
            ["Host"] = request.Host.ToString(),
            ["ContentType"] = request.ContentType,
            ["ForwardedFor"] = HeaderText(request, "X-Forwarded-For"),
            ["UserAgent"] = HeaderText(request, HeaderNames.UserAgent),
            ["AcceptLanguage"] = HeaderText(request, HeaderNames.AcceptLanguage),
            ["Origin"] = ClientOrigin(request),
            ["Referer"] = LogFields.Text(HostOf(request.Headers.Referer.ToString()), _options.MaxHeaderValueLength),
            ["ClientChannel"] = HeaderText(request, _options.ClientChannelHeader),
            ["ClientVersion"] = HeaderText(request, _options.ClientVersionHeader),
            ["ClientLat"] = HeaderText(request, _options.ClientLatHeader),
            ["ClientLong"] = HeaderText(request, _options.ClientLongHeader),
            ["RequestHeaders"] = Headers(request.Headers, isResponse: false),
            ["RequestBody"] = requestBody,
        };
        foreach (IRequestLogEnricher enricher in _enrichers)
        {
            enricher.Enrich(context, logType, requestFields);
        }

        return requestFields;
    }

    // The gateway leaves the logging to the services. It writes one log, for a request it answered itself: the service did not say it
    // logged it (it never got it, it is down, it did not answer). The body of the request is kept in a buffer for that, and only written
    // when the answer is a 5xx: what was asked when the service died.
    private async Task InvokeGatewayAsync(HttpContext context, string to)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        Stopwatch watch = Stopwatch.StartNew();
        bool buffersBody = _options.LogBodies && _masker is not null && IsJson(request.ContentType) && request.ContentLength is > 0 && request.ContentLength <= _options.MaxBodyReadBytes;
        if (buffersBody)
        {
            request.EnableBuffering();
        }

        Stream originalRequestBody = request.Body;
        CountingReadStream requestBytes = new(originalRequestBody);
        request.Body = requestBytes;
        IHttpResponseBodyFeature originalResponseBody = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        ResponseBodyTee responseBytes = new(originalResponseBody, () => CapturesResponseBody(context), _options.MaxBodyReadBytes);
        context.Features.Set<IHttpResponseBodyFeature>(responseBytes);

        bool loggedByService = false;
        response.OnStarting(() =>
        {
            // the sign is for the gateway, not for the client
            loggedByService = response.Headers.Remove(_options.LoggedByHeader);
            return Task.CompletedTask;
        });

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
            watch.Stop();

            // an answer without a body starts after this middleware is done: then the sign is still in the headers
            if (!loggedByService && !response.HasStarted && response.Headers.Remove(_options.LoggedByHeader))
            {
                loggedByService = true;
            }

            if (!loggedByService)
            {
                long readBytes = requestBytes.Count;
                string? requestBody = buffersBody && (failure is not null || response.StatusCode >= StatusCodes.Status500InternalServerError)
                    ? await ReadBufferedBodyAsync(request, BodyType(context)).ConfigureAwait(false)
                    : null;
                string? responseBody = responseBytes.Body is { } text ? MaskBody(text, context.Items.TryGetValue(ResponseShapeKey, out object? shape) ? shape as Type : null, _options.MaxResponseBodyLength) : null;
                WriteResponseLog(context, LogFlow.HttpClientName, to, watch.ElapsedMilliseconds, responseBody, readBytes, responseBytes.Count, failure, RequestFields(context, LogTypes.HttpRequestResponse, requestBody, context.User));
            }

            request.Body = originalRequestBody;
            context.Features.Set(originalResponseBody);
        }

        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    // The request was buffered (EnableBuffering) while the service read it: read it again, from its start. A service that never read it
    // (it could not be reached) leaves the rest of it in the connection, so this reads asynchronously, and nothing that goes wrong here may
    // change the answer the client gets.
    private async Task<string?> ReadBufferedBodyAsync(HttpRequest request, Type? shape)
    {
        try
        {
            request.Body.Position = 0;
            using StreamReader reader = new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            return MaskBody(await reader.ReadToEndAsync().ConfigureAwait(false), shape, _options.MaxRequestBodyLength);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or BadHttpRequestException)
        {
            return null;
        }
    }

    private void WriteResponseLog(
        HttpContext context, string from, string to, long elapsedMs, string? responseBody, long requestBytes, long responseBytes, Exception? failure, Dictionary<string, object?>? combined = null)
    {
        HttpResponse response = context.Response;
        int status = response.StatusCode;
        string headers = Headers(response.Headers, isResponse: true);
        string? userId = UserId(context.User);
        GrpcResult? grpc = GrpcResult.From(context);
        IReadOnlyList<LoggedFile> files = Files(context, requestBytes, responseBytes);

        // The level is the status, wherever it is logged (the gateway, a service): 2xx and 3xx are Information, 4xx are Warnings (the caller's
        // known errors), 5xx are Errors. A service answers with the status its global exception handler decided, so the log of the
        // response says the same thing in every place it is written.
        context.Items.TryGetValue(HandledExceptionKey, out object? handled);
        Exception? answered = handled as Exception;
        LogLevel level = failure is not null || status >= StatusCodes.Status500InternalServerError ? LogLevel.Error
            : status >= StatusCodes.Status400BadRequest ? LogLevel.Warning
            : LogLevel.Information;
        if (grpc is not null && level == LogLevel.Information)
        {
            // a gRPC call always answers 200: how it went is in its own status
            level = grpc.Level;
        }

        Dictionary<string, object?> fields = new()
        {
            [LogFields.LogType] = combined is null ? LogTypes.HttpResponse : LogTypes.HttpRequestResponse,
            ["UserId"] = userId,
            ["UserRoles"] = Roles(context.User),
            ["ResponseBytes"] = responseBytes,
            ["ResponseHeaders"] = headers,
            ["ResponseBody"] = responseBody,
            ["GrpcStatus"] = grpc?.Status,
            ["ExceptionType"] = (failure ?? answered)?.GetType().FullName,
            ["ErrorCode"] = (failure ?? answered) is BusinessException business ? business.ErrorCode : null,
            ["Files"] = files.Count == 0 ? null : LogFields.Text(JsonSerializer.Serialize(files, s_headerJson), _options.MaxHeadersLength),
            ["FileCount"] = files.Count == 0 ? null : files.Count,
            ["FileBytes"] = files.Count == 0 ? null : files.Sum(file => file.Size),
        };

        foreach (IRequestLogEnricher enricher in _enrichers)
        {
            enricher.Enrich(context, LogTypes.HttpResponse, fields);
        }

        string details = grpc is not null ? grpc.Summary : FileSummary(files);
        if (combined is not null)
        {
            // one log for the whole request: what is known of it, and how it was answered
            foreach (KeyValuePair<string, object?> field in fields.Where(field => field.Key != LogFields.LogType))
            {
                combined[field.Key] = field.Value;
            }

            string? shownPath = LogFields.Text(context.Request.Path.Value, _options.MaxUrlLength);
            using (logger.BeginScope(combined))
            {
                LogRequestResponse(logger, level, failure, from, to, context.Request.Method, shownPath, status, elapsedMs, details);
            }

            return;
        }

        using (logger.BeginScope(fields))
        {
            LogResponse(logger, level, failure, from, to, status, elapsedMs, details);
        }
    }

    // the files that went through: what the application read from a form, a raw upload, a download
    private static List<LoggedFile> Files(HttpContext context, long requestBytes, long responseBytes)
    {
        List<LoggedFile> files = [];
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        // a form is only there if the application read it: reading it here would buffer an upload nobody asked to buffer
        if (context.Features.Get<IFormFeature>()?.Form is { Files.Count: > 0 } form)
        {
            files.AddRange(form.Files.Select(file => new LoggedFile("upload", file.Name, FileNameOf(file.FileName), file.Length, file.ContentType)));
        }
        else if (IsRaw(request.ContentType) && requestBytes > 0 && request.Method is "POST" or "PUT" or "PATCH")
        {
            files.Add(new LoggedFile("upload", null, DispositionFileName(request.Headers.ContentDisposition), requestBytes, request.ContentType));
        }

        string? disposition = response.Headers.ContentDisposition;
        FileResultLogFilter.DownloadedFile? served = context.Items.TryGetValue(FileResultLogFilter.ItemKey, out object? item) ? item as FileResultLogFilter.DownloadedFile : null;
        bool isDownload = response.StatusCode is >= 200 and < 300 && responseBytes > 0 && (served is not null || disposition is not null || IsRaw(response.ContentType));
        if (isDownload)
        {
            files.Add(new LoggedFile("download", null, served?.FileName ?? DispositionFileName(disposition), responseBytes, response.ContentType));
        }

        return files;
    }

    private static string FileSummary(IReadOnlyList<LoggedFile> files)
        => files.Count == 0
            ? string.Empty
            : ", " + string.Join("; ", files.Select(file => string.Create(CultureInfo.InvariantCulture, $"{file.Direction} {file.FileName ?? file.Name ?? "(no name)"} ({file.Size} bytes)")));

    private static string? FileNameOf(string? name) => string.IsNullOrWhiteSpace(name) ? null : name;

    // a name the client chose is text from outside: it is cut like every other field
    private static string? DispositionFileName(string? header)
    {
        if (header is null || !ContentDispositionHeaderValue.TryParse(header, out ContentDispositionHeaderValue? parsed))
        {
            return null;
        }

        string? name = parsed.FileNameStar.HasValue ? parsed.FileNameStar.Value : parsed.FileName.Value;
        return string.IsNullOrWhiteSpace(name) ? null : LogFields.Text(name.Trim('"'), 256);
    }

    // not text, not JSON, not a form, not a gRPC message: bytes of a file
    private static bool IsRaw(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType)
           && !contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
           && !contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
           && !contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
           && !contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)
           && !contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
           && !contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase)
           && !contentType.StartsWith(GrpcContentType, StringComparison.OrdinalIgnoreCase);

    private sealed record LoggedFile(string Direction, string? Name, string? FileName, long Size, string? ContentType);

    /// <summary>How a gRPC call went: it answers HTTP 200 whatever happened, the result is the <c>grpc-status</c> header or trailer.</summary>
    private sealed record GrpcResult(int Status, string? Message)
    {
        public LogLevel Level => Status switch
        {
            0 => LogLevel.Information,
            1 or 3 or 5 or 6 or 7 or 9 or 11 or 16 => LogLevel.Warning,
            _ => LogLevel.Error,
        };

        public string Summary => string.Create(CultureInfo.InvariantCulture, $", grpc-status {Status}{(string.IsNullOrEmpty(Message) ? string.Empty : $" ({Message})")}");

        public static GrpcResult? From(HttpContext context)
        {
            if (!IsGrpc(context.Request))
            {
                return null;
            }

            // an error before the first message is "trailers only": the status is in the headers
            IHeaderDictionary? trailers = context.Features.Get<IHttpResponseTrailersFeature>()?.Trailers;
            string? status = context.Response.Headers["grpc-status"].FirstOrDefault() ?? trailers?["grpc-status"].FirstOrDefault();
            string? message = context.Response.Headers["grpc-message"].FirstOrDefault() ?? trailers?["grpc-message"].FirstOrDefault();
            return int.TryParse(status, NumberStyles.None, CultureInfo.InvariantCulture, out int code) ? new GrpcResult(code, LogFields.Text(message, 200)) : null;
        }
    }

    private static bool IsGrpc(HttpRequest request)
        => request.ContentType?.StartsWith(GrpcContentType, StringComparison.OrdinalIgnoreCase) == true;

    // The user is only known once the request is authenticated, which the pipeline does after this middleware. Authentication is asked for
    // here, early: the handler remembers its answer for the request, so asking again later costs nothing. Whatever goes wrong here is no
    // reason to fail the request: the log is then written without a user.
    private async Task<ClaimsPrincipal?> AuthenticatedUserAsync(HttpContext context)
    {
        try
        {
            if (_schemes is null || await _schemes.GetDefaultAuthenticateSchemeAsync().ConfigureAwait(false) is null)
            {
                return null;
            }

            AuthenticateResult result = await context.AuthenticateAsync().ConfigureAwait(false);
            return result.Succeeded ? result.Principal : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static string? UserId(ClaimsPrincipal? user)
        => user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

    // the roles of the user, as the old logs had them: distinct, comma separated; a JWT says "role", the platform maps it to ClaimTypes.Role
    private string? Roles(ClaimsPrincipal? user)
    {
        string roles = user is null ? string.Empty : string.Join(',', user.Claims.Where(claim => claim.Type is "role" or ClaimTypes.Role).Select(claim => claim.Value).Distinct());
        return roles.Length == 0 ? null : LogFields.Text(roles, _options.MaxHeaderValueLength);
    }

    private string? HeaderText(HttpRequest request, string name)
        => request.Headers[name].ToString() is { Length: > 0 } value ? LogFields.Text(value, _options.MaxHeaderValueLength) : null;

    // who is asking, as a host: the Origin, else the Referer, else the From header; without the scheme and "www."
    private string? ClientOrigin(HttpRequest request)
        => LogFields.Text(HostOf(request.Headers.Origin.ToString()) ?? HostOf(request.Headers.Referer.ToString()) ?? HostOf(request.Headers.From.ToString()), _options.MaxHeaderValueLength);

    internal static string? HostOf(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string host = value.ToLowerInvariant()
            .Replace("http://", string.Empty, StringComparison.Ordinal)
            .Replace("https://", string.Empty, StringComparison.Ordinal)
            .Replace("www.", string.Empty, StringComparison.Ordinal)
            .Split('/')[0];
        return host is "" or "null" ? null : host;
    }

    private string CorrelationId(HttpRequest request)
    {
        string? incoming = request.Headers[_options.CorrelationIdHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(incoming) ? LogFields.NewCorrelationId() : LogFields.Text(incoming.Trim(), MaxCorrelationIdLength)!;
    }

    private bool IsLogged(HttpRequest request)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        PathString path = request.Path;
        if (!_options.LogHealthChecks && _options.EffectiveHealthCheckPaths.Any(health => path.StartsWithSegments(health, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return !_options.ExcludedPaths.Any(excluded => path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <c>controller-action</c> in lower case for a controller action (<c>token-login</c>); for anything else (a minimal API endpoint, a
    /// proxied route) the request path in lower case, as it was asked for.
    /// </summary>
    internal static string StepName(HttpContext context)
    {
        ControllerActionDescriptor? action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is not null)
        {
            return LogFields.StepName(action.ControllerName, action.ActionName);
        }

        // gRPC: /package.Service/Method is service-method, like a controller action
        string[] segments = (context.Request.Path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (IsGrpc(context.Request) && segments.Length == 2)
        {
            return LogFields.StepName(segments[0][(segments[0].LastIndexOf('.') + 1)..], segments[1]);
        }

        return context.Request.Path.Value?.ToLowerInvariant() ?? "/";
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request, Type? shape)
    {
        if (!IsJson(request.ContentType) || request.ContentLength is null or 0 || request.ContentLength > _options.MaxBodyReadBytes)
        {
            return null;
        }

        request.EnableBuffering();
        using StreamReader reader = new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string text = await reader.ReadToEndAsync().ConfigureAwait(false);
        request.Body.Position = 0;
        return MaskBody(text, shape, _options.MaxRequestBodyLength);
    }

    private string? MaskBody(string text, Type? shape, int maxLength)
        => string.IsNullOrWhiteSpace(text) ? null : LogFields.Text(_masker!.MaskText(text, shape), maxLength);

    // the class the action reads its body into: what it marks as sensitive is hidden in the logged request
    private static Type? BodyType(HttpContext context)
    {
        IList<ParameterDescriptor>? parameters = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>()?.Parameters;
        return parameters?.FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)?.ParameterType;
    }

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

        return LogFields.Text(JsonSerializer.Serialize(shown, s_headerJson), _options.MaxHeadersLength)!;
    }

    private string HeaderValue(string name, string value, bool isResponse)
    {
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

    [LoggerMessage(EventId = 3000, EventName = "HTTP_REQUEST", Level = LogLevel.Information, Message = "{From} -> {To}: {Method} {RequestPath}")]
    private static partial void LogRequest(ILogger logger, string from, string to, string method, string? requestPath);

    // the level is the outcome: 5xx and exceptions are errors, 4xx are warnings
    [LoggerMessage(EventId = 3002, EventName = "HTTP_REQUEST_RESPONSE", Message = "{From} -> {To}: {Method} {RequestPath} {Status} in {ElapsedMs} ms{Details}")]
    private static partial void LogRequestResponse(ILogger logger, LogLevel level, Exception? exception, string from, string to, string method, string? requestPath, int status, long elapsedMs, string details);

    // Details: what is worth a word next to the status: the files that went through, a gRPC status
    [LoggerMessage(EventId = 3001, EventName = "HTTP_RESPONSE", Message = "{From} -> {To}: {Status} in {ElapsedMs} ms{Details}")]
    private static partial void LogResponse(ILogger logger, LogLevel level, Exception? exception, string from, string to, int status, long elapsedMs, string details);
}
