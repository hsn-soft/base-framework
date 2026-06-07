using System.Diagnostics;
using Hhs.Shared.Hosting.Helpers;
using HsnSoft.Base.AspNetCore.Logging;
using HsnSoft.Base.AspNetCore.Settings;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Tracing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hhs.Shared.Hosting.Middlewares;

public sealed class ApiRequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestResponseLogger _logger;
    private readonly ILogMasker _masker;
    private readonly ITraceAccesor _traceAccesor;
    private readonly HostingSettings _settings;

    public ApiRequestResponseLoggingMiddleware(
        RequestDelegate next,
        IRequestResponseLogger logger,
        ILogMasker masker,
        ITraceAccesor traceAccesor,
        IOptions<HostingSettings> settings)
    {
        _next = next;
        _logger = logger;
        _masker = masker;
        _traceAccesor = traceAccesor;
        _settings = settings.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!_settings.IsEnabledRequestResponseLogger)
        {
            await _next(context);
            return;
        }

        string path = context.Request.Path.ToString().ToLowerInvariant();
        if (!_settings.IsEnabledHealthCheckRequestLogger &&
            (path.Contains("startupcheck") || path.Contains("livenesscheck") || path.Contains("readinesscheck")))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var request = context.Request;
        var response = context.Response;

        string? requestBody = null;
        if (ShouldCaptureRequestBody(request))
        {
            requestBody = await ReadRequestBodyAsync(request, _settings.MaxLoggedRequestBodySizeBytes);

            if (IsJsonContentType(request.ContentType))
            {
                requestBody = _masker.MaskText(requestBody);
            }

            requestBody = TruncateText(requestBody, _settings.MaxLoggedRequestBodySizeBytes);
        }

        string? responseBody = null;
        Stream? originalBody = null;
        MemoryStream? captureStream = null;

        if (ShouldCaptureResponseBody(request))
        {
            originalBody = response.Body;
            captureStream = new MemoryStream();
            response.Body = captureStream;
        }

        Exception? pipelineException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            pipelineException = ex;
        }
        finally
        {
            sw.Stop();

            if (captureStream != null && originalBody != null)
            {
                try
                {
                    captureStream.Position = 0;

                    using var reader = new StreamReader(captureStream, leaveOpen: true);

                    responseBody = await reader.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        if (IsJsonContentType(response.ContentType))
                        {
                            responseBody = _masker.MaskText(responseBody);
                        }

                        responseBody = TruncateText(responseBody, _settings.MaxLoggedResponseBodySizeBytes);
                    }

                    bool canHaveBody =
                        response.StatusCode != StatusCodes.Status204NoContent &&
                        response.StatusCode != StatusCodes.Status304NotModified &&
                        request.Method != HttpMethods.Head;

                    if (canHaveBody)
                    {
                        captureStream.Position = 0;
                        await captureStream.CopyToAsync(originalBody);
                    }
                }
                finally
                {
                    response.Body = originalBody;
                    await captureStream.DisposeAsync();
                }
            }

            var log = BuildLogModel(context, requestBody, responseBody, sw.ElapsedMilliseconds);

            if (pipelineException != null || response.StatusCode >= 500)
            {
                _logger.RequestResponseErrorLog(log);
            }
            else if (response.StatusCode >= 400)
            {
                _logger.RequestResponseWarnLog(log);
            }
            else
            {
                _logger.RequestResponseInfoLog(log);
            }
        }
    }

    private RequestResponseLogModel BuildLogModel(HttpContext context, string? requestBody, string? responseBody, long elapsedMs)
    {
        var request = context.Request;
        var response = context.Response;

        var model = new RequestResponseLogModel
        {
            TraceId = context.TraceIdentifier,
            CorrelationId = _traceAccesor.GetCorrelationId(),
            ClientInfo = new ClientInfoLogDetail
            {
                RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
                ForwardedFor = TruncateText(request.Headers["X-Forwarded-For"].ToString(), _settings.MaxLoggedHeaderValueLength),
                UserAgent = TruncateText(request.Headers.UserAgent.ToString(), _settings.MaxLoggedHeaderValueLength),
                DeviceType = UserAgentProvider.GetUserAgentDetails(request.Headers.UserAgent.ToString())?.deviceType,
                AcceptLanguage = TruncateText(request.Headers.AcceptLanguage.ToString(), _settings.MaxLoggedHeaderValueLength),
                Origin = TruncateText(NormalizedClientOrigin(request.HttpContext.Request), _settings.MaxLoggedHeaderValueLength),
                Referer = TruncateText(NormalizedClientReferer(request.HttpContext.Request), _settings.MaxLoggedHeaderValueLength),
                UserId = _traceAccesor.GetUserId(),
                UserRoles = string.Join(",", _traceAccesor.GetUserRoles()),
                ClientLat = _traceAccesor.GetClientLat(),
                ClientLong = _traceAccesor.GetClientLong(),
                ClientChannel = _traceAccesor.GetClientChannel(),
                ClientVersion = _traceAccesor.GetClientVersion()
            },
            RequestInfo = new RequestInfoLogDetail
            {
                RequestDateTimeUtc = DateTime.UtcNow,
                RequestMethod = request.Method,
                RequestPath = request.Path,
                RequestQuery = request.QueryString.ToString(),
                RequestScheme = request.Scheme,
                RequestHost = request.Host.ToString(),
                RequestContentType = request.ContentType,
                RequestContentLength = request.ContentLength ?? 0,
                RequestHeaders = FormatRequestHeaders(request.Headers),
                RequestBody = requestBody
            },
            ResponseInfo = new ResponseInfoLogDetail
            {
                ResponseDateTimeUtc = DateTime.UtcNow,
                ResponseStatus = response.StatusCode,
                ResponseContentType = response.ContentType,
                ResponseContentLength = response.ContentLength ?? 0,
                ResponseHeaders = FormatResponseHeaders(response.Headers),
                ResponseBody = responseBody
            },
            RequestResponseWorkingTimeMs = elapsedMs,
        };

        return model;
    }

    [CanBeNull]
    private static string NormalizedClientOrigin(HttpRequest req)
    {
        string? clientOrigin = null;
        if (req.Headers.TryGetValue("Origin", out var originHost))
        {
            clientOrigin = originHost.ToString();
            if (!string.IsNullOrWhiteSpace(clientOrigin))
            {
                clientOrigin = clientOrigin.ToLower()
                    .Replace("http://", "")
                    .Replace("https://", "")
                    .Replace("www.", "")
                    .Replace("null", "")
                    .Split("/")[0];
            }
        }

        if (string.IsNullOrWhiteSpace(clientOrigin))
        {
            string? clientReferer = NormalizedClientReferer(req);
            if (!string.IsNullOrWhiteSpace(clientReferer))
            {
                clientOrigin = clientReferer;
            }
        }

        if (!string.IsNullOrWhiteSpace(clientOrigin) || !req.Headers.TryGetValue("From", out var fromHost))
        {
            return clientOrigin;
        }

        clientOrigin = fromHost.ToString();
        if (!string.IsNullOrWhiteSpace(clientOrigin))
        {
            clientOrigin = clientOrigin.ToLower()
                .Replace("http://", "")
                .Replace("https://", "")
                .Replace("www.", "")
                .Replace("null", "")
                .Split("/")[0];
        }

        return clientOrigin;
    }

    [CanBeNull]
    private static string NormalizedClientReferer(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("Referer", out var refererHost))
        {
            return null;
        }

        string? clientReferer = refererHost.ToString();
        if (!string.IsNullOrWhiteSpace(clientReferer))
        {
            return clientReferer.ToLower()
                .Replace("http://", "")
                .Replace("https://", "")
                .Replace("www.", "")
                .Replace("null", "")
                .Split("/")[0];
        }

        return null;
    }

    private static bool ShouldCaptureRequestBody(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return false;

        string contentType = request.ContentType ?? string.Empty;

        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return false;

        return !contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldCaptureResponseBody(HttpRequest request)
    {
        string path = request.Path.ToString().ToLowerInvariant();

        return !path.StartsWith("/swagger") && !path.StartsWith("/health");
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request, int maxBytes)
    {
        request.EnableBuffering(bufferThreshold: 1024 * 30, bufferLimit: maxBytes);
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        string text = await reader.ReadToEndAsync();

        request.Body.Position = 0;
        return text;
    }

    private Dictionary<string, string> FormatRequestHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            result[header.Key] = FormatRequestHeaderValue(header.Key, header.Value.ToString());
        }

        return result;
    }

    private Dictionary<string, string> FormatResponseHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            result[header.Key] = FormatResponseHeaderValue(header.Key, header.Value.ToString());
        }

        return result;
    }

    private string FormatRequestHeaderValue(string headerName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            return FormatAuthorizationHeader(value);
        }

        return headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ? FormatCookieHeader(value) : TruncateText(value, _settings.MaxLoggedHeaderValueLength);
    }

    private string FormatResponseHeaderValue(string headerName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ? FormatSetCookieHeader(value) : TruncateText(value, _settings.MaxLoggedHeaderValueLength);
    }

    private static string FormatAuthorizationHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        const string bearerPrefix = "Bearer ";

        if (value.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string token = value[bearerPrefix.Length..].Trim();
            return $"Bearer (length: {token.Length})";
        }

        return $"AuthHeader (length: {value.Length})";
    }

    private static string FormatCookieHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var names = value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split('=', 2)[0].Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10) // Max cookie count
            .Select(x => x.Length > 30 ? x[..30] + "..." : x) // Max cookie name length
            .ToList();

        return names.Count == 0 ? "Cookies: <unknown>" : $"Cookies: {string.Join(", ", names)}";
    }

    private static string FormatSetCookieHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string firstPart = value.Split(';', 2, StringSplitOptions.TrimEntries)[0];

        if (string.IsNullOrWhiteSpace(firstPart))
            return "Set-Cookie: <unknown>";

        string name = firstPart.Split('=', 2)[0].Trim();

        return string.IsNullOrWhiteSpace(name) ? "Set-Cookie: <unknown>" : $"Set-Cookie: {(name.Length > 30 ? name[..30] + "..." : name)}";
    }

    private static string TruncateText(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value[..maxLength] + "...[truncated]";
    }
}