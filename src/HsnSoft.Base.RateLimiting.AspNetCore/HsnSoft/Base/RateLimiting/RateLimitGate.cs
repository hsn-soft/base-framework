using System;
using System.Globalization;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.RateLimiting;

internal static partial class RateLimitGate
{
    /// <summary>Counts the request: <see langword="null"/> when it is within the limit, else the answer of "too many" (the header is set, the caller gives the body its status).</summary>
    internal static async ValueTask<BaseResponse?> CheckAsync(HttpContext http, string name, int codePermits, int codeWindowSeconds, RateLimitBy by)
    {
        RateLimitOptions options = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        (int permits, TimeSpan window) = options.Resolve(name, codePermits, codeWindowSeconds);
        string key = KeyOf(http, by);
        RateLimitResult result;
        try
        {
            result = await http.RequestServices.GetRequiredService<IRateLimiter>().TryAcquireAsync(name, key, permits, window, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (options.FailOpen && exception is not OperationCanceledException)
        {
            // the store of the counts is gone and the settings say the request matters more than its limit
            LogStoreFailed(http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HsnSoft.Base.RateLimiting"), exception, name);
            return null;
        }

        return result.Allowed ? null : TooMany(http, result);
    }

    internal static string KeyOf(HttpContext http, RateLimitBy by)
        => by == RateLimitBy.User && http.RequestServices.GetService<ICurrentUser>() is { Id: { } id }
            ? "user:" + id.ToString("N")
            : "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static BaseResponse TooMany(HttpContext http, RateLimitResult result)
    {
        int seconds = Math.Max(1, (int)Math.Ceiling(result.RetryAfter.TotalSeconds));
        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        return new BaseResponse { StatusCode = StatusCodes.Status429TooManyRequests, StatusMessages = ["Too many requests. Try again later."], TraceId = http.TraceIdentifier, ErrorCode = "rate_limit_exceeded" };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The store of the rate limit could not be reached; the request of the limit '{Name}' is let through (RateLimiting:FailOpen)")]
    private static partial void LogStoreFailed(ILogger logger, Exception exception, string name);
}
